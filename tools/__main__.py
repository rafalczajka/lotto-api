import csv
import datetime
import os
import time
from argparse import ArgumentParser, ArgumentTypeError
from pathlib import Path
from zoneinfo import ZoneInfo

import requests
from azure.data.tables import TableServiceClient, UpdateMode
from dotenv import load_dotenv

LOTTO_ENDPOINT = 'https://developers.lotto.pl/api/open/v1/lotteries/draw-results/by-date-per-game'

TABLE_NAME = 'LottoDrawResults'

DEFAULT_CSV_FILE_NAME = 'data.csv'
DEFAULT_START_DATE = '2000-01-01'
DEFAULT_BATCH_SIZE = 100

REQUEST_DELAY_SEC = 1
MAX_HTTP_500_RETRIES = 3
TIMEZONE = ZoneInfo('Europe/Warsaw')

ROOT_DIR = Path(__file__).resolve().parent.parent

load_dotenv(ROOT_DIR / '.env')


def require_env(name: str) -> str:
    value = os.getenv(name)

    if not value:
        raise RuntimeError(f'Missing required environment variable: {name}')

    return value


def parse_batch_size(value: str) -> int:
    try:
        size = int(value)
    except ValueError as error:
        raise ArgumentTypeError('must be an integer') from error

    if not 1 <= size <= 100:
        raise ArgumentTypeError('must be between 1 and 100')

    return size


def fetch_draw_results(date: str) -> tuple[int, dict]:
    headers = {
        'User-Agent': require_env('USER_AGENT'),
        'secret': require_env('LOTTO_API_KEY'),
    }

    params = {
        'gameType': 'Lotto',
        'drawDate': date,
        'index': 1,
        'size': 100,
        'sort': 'drawSystemId',
        'order': 'ASC',
    }

    response = requests.get(
        LOTTO_ENDPOINT,
        headers=headers,
        params=params,
        timeout=10,
    )

    status_code = response.status_code

    if status_code in (404, 500):
        return status_code, {}

    response.raise_for_status()

    return status_code, response.json()


def fetch_data(filename: Path, start_date: str) -> None:
    date = datetime.date.fromisoformat(start_date)
    end_date = datetime.datetime.now(tz=TIMEZONE).date()

    retries = 0

    with filename.open('w', newline='', encoding='utf-8') as file:
        writer = csv.writer(file)

        writer.writerow([
            'DrawDate',
            'LottoNumbers',
            'PlusNumbers',
        ])

        file.flush()

        while date <= end_date:
            date_str = date.isoformat()

            status_code, data = fetch_draw_results(date_str)

            if data and data.get('items'):
                game_results = {
                    item['gameType']: item['results'][0]['resultsJson']
                    for item in data['items']
                }

                numbers = game_results.get('Lotto', [])
                plus_numbers = game_results.get('LottoPlus', [])

                writer.writerow([
                    date_str,
                    ','.join(map(str, numbers)),
                    ','.join(map(str, plus_numbers)),
                ])

                file.flush()

                print(f'Numbers: {date_str} -> {numbers}, (plus: {plus_numbers})')

            elif status_code == 500:
                if retries >= MAX_HTTP_500_RETRIES:
                    raise RuntimeError(
                        f'Request failed with status code {status_code} for {date_str} '
                        f'after {MAX_HTTP_500_RETRIES} retries.'
                    )

                retries += 1
                delay = REQUEST_DELAY_SEC * 10

                print(
                    f'Request failed (response code: {status_code}). '
                    f'Retrying ({retries}/{MAX_HTTP_500_RETRIES}) after {delay} seconds...'
                )

                time.sleep(delay)
                continue

            else:
                print(f'No data for {date_str}, skipping...')

            date += datetime.timedelta(days=1)
            retries = 0
            time.sleep(REQUEST_DELAY_SEC)

    print(f'Data saved to {filename}')


def upload_batch(table_client, batch: list) -> None:
    table_client.submit_transaction(batch)
    print(f'Uploaded batch of {len(batch)} records.')


def upload_data(filename: Path, batch_size: int) -> None:
    connection_string = require_env('STORAGE_CONNECTION_STRING')

    table_service = TableServiceClient.from_connection_string(connection_string)
    table_client = table_service.get_table_client(TABLE_NAME)

    batch = []
    max_date = datetime.date.max

    with filename.open('r', encoding='utf-8') as file:
        reader = csv.DictReader(file)

        for row in reader:
            draw_date_str = row['DrawDate']
            draw_date = datetime.date.fromisoformat(draw_date_str)
            reversed_draw_date = datetime.date.min + (max_date - draw_date)

            entity = {
                'PartitionKey': 'LottoData',
                'RowKey': reversed_draw_date.strftime('%Y%m%d'),
                'DrawDate': draw_date_str,
                'LottoNumbers': row['LottoNumbers'],
                'PlusNumbers': row['PlusNumbers'],
            }

            batch.append(('upsert', entity, {'mode': UpdateMode.REPLACE}))

            if len(batch) >= batch_size:
                upload_batch(table_client, batch)
                batch = []

    if batch:
        upload_batch(table_client, batch)

    print('Upload completed successfully!')


def create_parser() -> ArgumentParser:
    parser = ArgumentParser(description='Tools for initializing Lotto data.')
    subparsers = parser.add_subparsers(dest='command', required=True)

    fetch_parser = subparsers.add_parser(
        'fetch',
        help='Fetch Lotto draw results and save them to CSV.',
    )

    fetch_parser.add_argument(
        'file',
        nargs='?',
        default=DEFAULT_CSV_FILE_NAME,
        type=Path,
        help=f'CSV output file (default: {DEFAULT_CSV_FILE_NAME})',
    )

    fetch_parser.add_argument(
        '--from',
        dest='start_date',
        default=DEFAULT_START_DATE,
        metavar='DATE',
        help=f'Date from which data collection starts (default: {DEFAULT_START_DATE})',
    )

    fetch_parser.set_defaults(handler=run_fetch)

    upload_parser = subparsers.add_parser(
        'upload',
        help='Upload draw results from CSV to Azure Table Storage.',
    )

    upload_parser.add_argument(
        'file',
        nargs='?',
        default=DEFAULT_CSV_FILE_NAME,
        type=Path,
        help=f'CSV input file (default: {DEFAULT_CSV_FILE_NAME})',
    )

    upload_parser.add_argument(
        '--batch-size',
        type=parse_batch_size,
        default=DEFAULT_BATCH_SIZE,
        help=f'Upload batch size (default: {DEFAULT_BATCH_SIZE})',
    )

    upload_parser.set_defaults(handler=run_upload)

    return parser


def run_fetch(args) -> None:
    try:
        fetch_data(args.file, args.start_date)
    except KeyboardInterrupt:
        print('\nFetch interrupted. Partial data has been saved.')
        raise SystemExit(130)


def run_upload(args) -> None:
    try:
        upload_data(args.file, args.batch_size)
    except KeyboardInterrupt:
        print('\nUpload interrupted. Previously uploaded batches have been saved.')
        raise SystemExit(130)


def main() -> None:
    parser = create_parser()
    args = parser.parse_args()
    args.handler(args)


if __name__ == '__main__':
    main()

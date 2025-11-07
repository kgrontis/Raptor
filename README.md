# Raptor - Web API Load Testing Tool

A console-based load testing tool for web APIs.

## Features

- Configurable concurrent request execution
- Statistics (latency, status codes, throughput, error rates)
- Real-time progress indicator

## Prerequisites

- **.NET 8 SDK** or later
- **Windows, Linux, or macOS** (any OS supported by .NET 8)

## Installation

### Install as Global Tool

1. **Build and package the tool:**

   ```bash
   cd src/Raptor.Cli
   dotnet pack -c Release
   ```

2. **Install globally from the generated package:**

   ```bash
   dotnet tool install --global --add-source ./nupkg Raptor.Cli
   ```

3. **Verify installation:**
   ```bash
   raptor --help
   ```

### Update Global Tool

To update an existing installation:

```bash
cd src/Raptor.Cli
dotnet pack -c Release
dotnet tool update --global --add-source ./nupkg Raptor.Cli
```

### Uninstall Global Tool

```bash
dotnet tool uninstall --global Raptor.Cli
```

## Running

After installation, use the `raptor` command from any directory:

```bash
raptor --url https://api.example.com/v1/users --concurrency 10 --duration 30
```

Or run directly from source:

```bash
cd src/Raptor.Cli
dotnet run -- --url https://api.example.com/v1/users --concurrency 10 --duration 30
```

## Arguments

### Required Arguments

- `--url <url>` - Target URL to load test (e.g., `https://api.example.com/users`)

- `--concurrency <n>` - Number of concurrent requests to send (e.g., `10`, `50`, `100`)

  - Range: 1 to 65,535
  - Default: 1

- `--duration <seconds>` OR `--requests <n>` - Specify either:

  - `--duration <seconds>` - Run for a specific duration (e.g., `--duration 60` runs for 60 seconds)
    - Range: 1 to 65,535 seconds
  - `--requests <n>` - Run until a specific number of requests complete (e.g., `--requests 1000`)
    - Range: 1 to 65,535 requests

  **Note:** Use either `--duration` or `--requests`, not both.

### Optional Arguments

- `--method <method>` - HTTP method (GET, POST, PUT, DELETE). Default: `GET`

  - Case-insensitive (GET, get, Get all work)

- `--body <json>` - Request body for POST/PUT requests (must be valid JSON)

  ```bash
  --body '{"name":"John","email":"john@example.com"}'
  ```

- `--headers <key:value,...>` - Custom HTTP headers as comma or semicolon-separated key:value pairs

  - Headers are automatically trimmed (whitespace around keys/values is removed)
  - Supports both comma (`,`) and semicolon (`;`) as delimiters

  ```bash
  --headers "Content-Type:application/json,Authorization:Bearer token123"
  # or
  --headers "Content-Type:application/json;Authorization:Bearer token123"
  ```

- `--help` or `-h` - Display usage information
  - Also displayed when running `raptor` with no arguments

## Usage Examples

### Basic Load Test

Run a GET request load test for 30 seconds with 10 concurrent requests:

```bash
raptor --url https://api.example.com/v1/users --concurrency 10 --duration 30
```

### Fixed Number of Requests

Run until 1000 requests complete:

```bash
raptor --url https://api.example.com/v1/users --concurrency 50 --requests 1000
```

### POST Request with JSON Body

Send POST requests with a JSON payload:

```bash
raptor --url https://api.example.com/v1/users \
  --method POST \
  --concurrency 20 \
  --duration 60 \
  --body '{"name":"John","email":"john@example.com"}' \
  --headers "Content-Type:application/json,Authorization:Bearer token123"
```

### Display Help

```bash
raptor --help
# or
raptor -h
# or simply
raptor
```

## Output

Raptor displays statistics including:

- Total requests completed
- Successful requests count
- Error count
- Test duration
- Requests per second (RPS)
- Response time percentiles (P50, P95, P99)
- Minimum, maximum, and average latency
- Status code distribution with percentages

### Example Output

```
=== Load Test Results ===

Summary:
  Total Requests:      1,000
  Successful:          995
  Errors:              5
  Duration:            30.00s
  Requests/sec:        33.33

Latency (ms):
  Min:              1284
  Max:              1381
  Avg:              1326
  P50:              1324
  P95:              1328
  P99:              1328

Status Codes:
    200: 995 (99.5%)
    500: 5 (0.5%)
```

## Statistics Collection

**Important**: The tool stores a limited number of results for latency percentile calculations. While `TotalRequests` counts all requests made, percentiles (P50, P95, P99) are calculated from stored samples. This ensures memory efficiency while maintaining statistical accuracy for typical load tests.

- **Total Requests**: Counts all requests made during the test
- **Latency Percentiles**: Calculated from a subset of stored results (capacity-limited)
- **Status Codes**: Tracks all status codes encountered
- **Error Count**: Counts all errors (network failures, timeouts, etc.)

## Exit Codes

- `0`: Success (test completed or cancelled gracefully)
- `1`: Error (invalid arguments or unexpected error)

## Cancellation

Press `Ctrl+C` to stop the test early. Final statistics will be displayed and the tool exits with code 0 (success).

## Troubleshooting

### Invalid Arguments

If you provide invalid arguments, the tool will display an error message and show usage information. Common issues:

- Missing required arguments (`--url`, `--concurrency`, `--duration` or `--requests`)
- Invalid values (non-numeric, zero, or negative numbers)
- Values exceeding maximum limits (65,535)
- Specifying both `--duration` and `--requests` (mutually exclusive)

### Error Messages

The tool provides clear error messages for:

- Missing option values
- Invalid numeric values
- Unsupported HTTP methods
- Invalid configuration combinations

## License

MIT License

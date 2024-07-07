# CS2 placenames

Extract map areas and their corresponding origin points to a json file.

```
Description:
  Extract placenames from CS2 map vpk files

Usage:
  placenames [<path>] [options]

Arguments:
  <path>  directory containing map vpk files []

Options:
  -o, --output <output>  output directory [default: /home/runie/striker-mono/cs2-placenames]
  -m, --merge            merge into one json file
  -f, --filter <filter>  regex filter [default: ^(ar|cs|de)((?!vanity).)*\.vpk$]
  -p, --pretty           indent output json
  --version              Show version information
  -?, -h, --help         Show help and usage information
```
# Sequence® Configuration Language

A [Visual Studio Code](https://code.visualstudio.com/) extension for
the [Sequence Configuration Language](https://sequence.sh) (SCL).

SCL is a configuration language designed to simplify automation
of cross-application forensic and e-discovery workflows.
It is designed to be powerful, containing most of the control flow
features of programming languages,
yet much easier to pick-up and use than traditional scripting or
programming languages.

A quick introduction to the language and its features can be found in the
[documentation](https://sequence.sh/docs/sequence-configuration-language).

SCL can be validated and executed using the open-source command line
application [Sequence](https://sequence.sh).

## Supported Features

- Syntax highlighting
- Hover
- Code completion for step names and parameters
- Error diagnostics
- Run and validate sequences

This extension is still in preview.

## SCL Examples

### Remove duplicate rows from a CSV file

```jsx
- FileRead 'C:\temp\data.csv'
| FromCSV
| RemoveDuplicates
| ToCSV
| FileWrite 'C:\temp\data_no-duplicates.csv'
```

### Get data from a SQL database and write to CSV

```jsx
- <ConnectionString> = CreateMySQLConnectionString
                         Server: 'localhost'
                         Database: 'database'
                         UserName: 'root'
                         Password: 'verysecret'

- SqlQuery
    ConnectionString: <ConnectionString>
    DatabaseType: DatabaseType.MariaDb
    Command: $"SELECT * FROM MYTABLE"
| ToCSV
| FileWrite 'C:\temp\mytable-export.csv'
```

For more examples and all the supported applications (through our
[connectors](https://sequence.sh/docs/connectors/core)
please see the [documentation](https://sequence.sh/docs/examples/core).

## Grammar and Interpreter

- [Core](https://gitlab.com/reductech/sequence/core) is the interpreter for SCL
- The grammar is defined using [ANTLR](https://www.antlr.org/)

## Running SCL

To run SCL when using this extension:

1. Open a `.scl` file.
2. Use the `SCL: Run Sequence` command.

## Documentation

Documentation is available at [https://sequence.sh](https://sequence.sh/docs/intro).

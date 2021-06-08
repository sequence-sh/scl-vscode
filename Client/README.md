# Sequence Configuration Language

This is a [VS Code](https://code.visualstudio.com/) extension for
the [Reductech](https://reductech.io/) Sequence Configuration Language (SCL).

SCL is the language that is used to define cross-application
forensic and e-discovery workflows. It is designed to be powerful,
containing most of the control flow features of programming languages,
yet much easier to pick-up and use than traditional scripting or
programming languages.

A quick introduction to the language and its features can be found in the
[documentation](https://docs.reductech.io/edr/how-to/scl/sequence-configuration-language.html).

SCL can be validated and executed using the open-source command line
application [EDR](https://gitlab.com/reductech/edr/edr/-/releases).

## Supported Features

- Syntax Highlighting
- Hover
- Code Completion for Step names and parameters
- Error Diagnostics

This extension is still in preview.

## SCL Example

To remove duplicate rows from a CSV file:

```perl
- FileRead 'C:\temp\data.csv'
| FromCSV
| ArrayDistinct <entity>
| ToCSV
| FileWrite 'C:\temp\data-NoDuplicates.csv'
```

## Grammar and Interpreter

- [Core](https://gitlab.com/reductech/edr/core) is the interpreter for SCL
- The grammar is defined using [ANTLR](https://www.antlr.org/)

## Running SCL

To run SCL when using this extension:

1. Download the latest release of EDR [here](https://gitlab.com/reductech/edr/edr/-/releases)
2. In VS Code settings, update the EDR path `Extensions > Reductech SCL > Edr: Path`
   or `reductech-scl.edr.path` in the JSON file.
3. Use the command `SCL: Run Sequence` from the command palette, or hit _F5_
   when in an SCL file.

## Documentation

Documentation is available at [docs.reductech.io](https://docs.reductech.io)

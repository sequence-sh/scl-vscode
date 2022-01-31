## SCL VS Code

This is the VS Code extension for SCL.

## To Build and Run

First, run `npm install` in the `.\Client` directory.

There are VS Code tasks defined to build both the client
and server. To use open the command palette (`F1`) then
go to `Tasks: Run Task`.

To Run SCL, press `F5`

## How to change settings

File > Preferences > Settings > Reductech-SCL

The are two settings properties

`connectors` determines which connectors will be used.

`nlog` controls the Run-SCL log output

This is the default configuration which writes both to a file and to the output window.

```json
  "nlog": {
    "throwConfigExceptions": true,
    "variables": {
      "logname": "..\\sequence"
    },
    "targets": {
      "fileTarget": {
        "type": "File",
        "fileName": "${basedir:fixtempdir=true}\\${logname}.log",
        "layout": "${date} ${level:uppercase=true} ${message} ${exception}"
      },
      "outputWindow": {
        "type": "OutputWindow",
        "layout": "${date} ${message}"
      }
    },
    "rules": [
      {
        "logger": "*",
        "minLevel": "Error",
        "writeTo": "fileTarget,outputWindow",
        "final": true
      },
      {
        "logger": "*",
        "minLevel": "Debug",
        "writeTo": "outputWindow"
      },
      {
        "logger": "*",
        "minLevel": "Debug",
        "writeTo": "fileTarget"
      }
    ]
  }
```

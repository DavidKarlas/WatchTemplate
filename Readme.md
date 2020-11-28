watch-template
=====

Dotnet tool to watch for file changes inside template folder and display changes inside browser in real-time.

## How to install

Use this command to install watch-template as a .NET global tool:

```
dotnet tool install -g watch-template
```

## How to use

 1) Navigate to folder which has `.template.config` folder that contains `template.json` file.
 1) Execute:
    ```
    watch-template
    ```
 1) This will start local ASP.NET server on https://localhost:5001.
 1) Open browser and enter https://localhost:5001.
 1) Modify and **save** template files in your favorite editor.
 1) Observe instant diff in your browser.

## How to develop

1) Open solution/project.
2) Set "Working Directory" to folder which has `.template.config` folder that contains `template.json` file.
3) Start debugging.

## License
`watch-template` is available under MIT license.

For more information see the [License file](LICENSE).
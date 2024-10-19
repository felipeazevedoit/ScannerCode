# ScannerCode Project

This project is a code scanner designed to inspect a specified directory, list `.cs` files, analyze their structure, and log the details of the analysis to a text file. The log file is generated dynamically based on the project's name and the current timestamp. This README outlines the steps to configure and run the scan, as well as details about how the log file is generated.

## Prerequisites

- .NET SDK 6.0 or later
- Visual Studio or any preferred C# IDE
- Basic knowledge of .NET configuration files (`App.config`)

## How to Configure and Run the Scan

### 1. Configure the `App.config` File

Before running the scanner, you must configure the `App.config` file to specify the project directory to be scanned and the output directory where the log will be saved.

In the `App.config`, there are two key settings that you need to adjust:

```xml
<configuration>
  <appSettings>
    <!-- Path to the project directory that should be scanned -->
    <add key="ProjectPath" value="C:\Path\To\Your\Project" />

    <!-- Directory where the log file will be saved -->
    <add key="LogOutputPath" value="C:\Path\To\Log\Directory" />
  </appSettings>
</configuration>
```

- **ProjectPath**: Set this to the absolute path of the directory you want the scanner to analyze. It should point to the root folder of your project.
- **LogOutputPath**: This is the directory where the scanner will save the log file. Ensure that the directory exists or the log will default to the current working directory.

### 2. Running the Scanner

Once you have configured the `App.config`, you can run the scanner via Visual Studio or the command line.

**Via Visual Studio**:
- Open the solution in Visual Studio.
- Ensure that your `App.config` is properly set up.
- Build and run the project.

**Via Command Line**:
- Navigate to the project folder in your terminal or command prompt.
- Run the following command:

  ```bash
  dotnet run
  ```

This will start the scan process.

### 3. How the Log File is Generated

- The scanner generates a log file dynamically based on the name of the project directory and the current date and time.
- The log file name follows the format: `{ProjectName}_scanOut_{YYYYMMDD_HHMMSS}.txt`.
  - Example: `MyProject_scanOut_20231019_153045.txt`
  
- The log file contains detailed information about the structure of the project, the files found, and the methods detected.

### 4. Output Location

- If the `LogOutputPath` is correctly configured in the `App.config`, the log will be saved in that directory. Otherwise, it will be saved in the current working directory from where the scanner was run.

## What the Scanner Does - Step-by-Step Process

Here is a detailed breakdown of the steps the scanner follows to collect the necessary data and generate the log:

1. **Read Configuration**:
   - The scanner starts by reading the `ProjectPath` and `LogOutputPath` from the `App.config`.
   
2. **Directory Check**:
   - It verifies that the directory specified by `ProjectPath` exists. If not, the process is aborted, and an error is logged.

3. **File Collection**:
   - The scanner recursively collects all `.cs` (C#) files from the project directory, ignoring certain directories like `bin`, `obj`, `.git`, and others that are not relevant to the analysis.
   
4. **Project Structure Logging**:
   - It logs the structure of the project, printing the directory tree, and showing all relevant `.cs` files.
   
5. **Method Analysis**:
   - The scanner analyzes each C# file and extracts the class and method definitions. For each method, it logs:
     - The return type
     - Method name
     - Parameters
     - Basic logic structure (loops, conditions, etc.)
   
6. **Dependency and Logic Analysis**:
   - The scanner identifies dependencies between methods, analyzing function calls and the logic structure within the methods, including loops, conditions, and try-catch blocks.

7. **Save Log**:
   - Once the scan is completed, all output that was printed to the console is saved into a dynamically named log file as described above.

8. **Completion**:
   - The scanner informs the user that the process is complete and displays the path where the log file has been saved.

## Example of Generated Log

Here is an example of how the log file content might look:

```
Project Structure:
|-- Controllers
|   |-- HomeController.cs
|-- Models
|   |-- User.cs

Scanning file: HomeController.cs
   - Class: HomeController
      -> public IActionResult Index()
         (No parameters)
         Contains if condition
         Calls method: UserService.GetUser()

Scanning file: User.cs
   - Class: User
      -> public string Name { get; set; }
```

## Troubleshooting

1. **Directory Not Found**:
   - Ensure that the `ProjectPath` in the `App.config` points to a valid directory. If the directory does not exist or is incorrectly typed, the scanner will not run.

2. **Log Not Being Generated**:
   - Verify that the `LogOutputPath` in `App.config` points to a valid directory. If the directory is invalid or inaccessible, the log will be saved in the current working directory.

3. **Missing Files**:
   - Ensure that the project contains `.cs` files. If no `.cs` files are found, ensure that the directory is correct and that the project contains relevant files.


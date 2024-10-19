using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

class Program
{
    static async Task Main(string[] args)
    {
        var configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        var projectPath = configuration.AppSettings.Settings["ProjectPath"].Value;

        if (!CheckDirectoryExists(projectPath))
            return;

        var programInstance = new Program();
        await programInstance.ScanProject(projectPath);

        Console.WriteLine("\nPressione Enter para fechar o console...");
        Console.ReadLine();
    }

    private static bool CheckDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Console.WriteLine($"Erro: O diretório {path} não existe.");
            return false;
        }
        return true;
    }

    public async Task ScanProject(string projectPath)
    {
        var files = Directory.GetFiles(projectPath, "*", SearchOption.AllDirectories)
            .Where(f => !f.Contains(@"\objects\") && !f.Contains(@"\obj\")).ToArray();

        Console.WriteLine($"O diretório contém {files.Length} arquivos (excluindo as pastas 'objects' e 'obj').");

        Console.WriteLine("\nEstrutura do projeto:");
        PrintDirectoryStructure(projectPath, 0);

        ListMethodsInFiles(files);

        await Task.CompletedTask;
    }

    private void ListMethodsInFiles(string[] files)
    {
        var csFiles = files.Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).ToArray();
        foreach (var file in csFiles)
        {
            Console.WriteLine($"|-- {Path.GetFileName(file)}");
            ListCsMethods(file);
        }
    }

    private void ListCsMethods(string filePath)
    {
        var code = File.ReadAllText(filePath);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetCompilationUnitRoot();

        // Detect classes and methods in the file
        var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

        foreach (var @class in classDeclarations)
        {
            Console.WriteLine($"    |-- Class: {@class.Identifier.Text}");

            // Mapeamento de Estrutura de Dados: Detecção de classes de modelo e anotações
            DetectModelAttributes(@class);

            var methodDeclarations = @class.DescendantNodes().OfType<MethodDeclarationSyntax>();

            foreach (var method in methodDeclarations)
            {
                var returnType = method.ReturnType.ToString();
                var methodName = method.Identifier.Text;
                var parameters = method.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}").ToArray();

                // Mapeamento de Estilo de Codificação: Verificação de convenções de nomenclatura
                AnalyzeNamingConventions(methodName);

                Console.WriteLine($"        |-- {returnType} {methodName}({string.Join(", ", parameters)})");

                AnalyzeMethodLogic(method, methodName);

                ListMethodDependencies(method);
            }
        }
    }

    private void DetectModelAttributes(ClassDeclarationSyntax classDeclaration)
    {
        var modelAttributes = new[] { "Required", "MaxLength", "ForeignKey" };
        var attributes = classDeclaration.AttributeLists.SelectMany(attrList => attrList.Attributes);

        foreach (var attribute in attributes)
        {
            var attributeName = attribute.Name.ToString();
            if (modelAttributes.Contains(attributeName))
            {
                Console.WriteLine($"        -> Model attribute detected: {attributeName}");
            }
        }
    }

    private void AnalyzeNamingConventions(string methodName)
    {
        if (!char.IsUpper(methodName[0]))
        {
            Console.WriteLine($"        -> Naming convention warning: Method '{methodName}' should be PascalCase.");
        }
    }

    private void AnalyzeMethodLogic(MethodDeclarationSyntax method, string methodName)
    {
        Console.WriteLine($"\nAnalyzing method: {methodName}");

        var body = method.Body;
        if (body != null)
        {
            Console.WriteLine("Method logic:");

            var variables = new Dictionary<string, string>();

            foreach (var statement in body.Statements)
            {
                // Análise do uso de loops e condições
                if (statement is ForStatementSyntax forStatement)
                {
                    Console.WriteLine(" - Contains a 'for' loop");
                    AnalyzeLoop(forStatement);
                }
                else if (statement is WhileStatementSyntax whileStatement)
                {
                    Console.WriteLine(" - Contains a 'while' loop");
                    AnalyzeLoop(whileStatement);
                }
                else if (statement is IfStatementSyntax ifStatement)
                {
                    Console.WriteLine(" - Contains an 'if' condition");
                    AnalyzeCondition(ifStatement);
                }
                else if (statement is TryStatementSyntax tryStatement)
                {
                    Console.WriteLine(" - Contains a 'try-catch' block");
                    AnalyzeTryCatch(tryStatement);
                }
                else if (statement is ExpressionStatementSyntax expressionStatement)
                {
                    var expression = expressionStatement.Expression;

                    if (expression is InvocationExpressionSyntax invocation)
                    {
                        var methodCalled = invocation.Expression.ToString();
                        Console.WriteLine($" - Calls method: {methodCalled}");
                        AnalyzeInvocation(invocation, variables);
                    }
                    else if (expression is AssignmentExpressionSyntax assignment)
                    {
                        AnalyzeAssignment(assignment, variables);
                    }
                    else if (expression is BinaryExpressionSyntax binaryExpression)
                    {
                        AnalyzeBinaryExpression(binaryExpression, variables);
                    }
                    else if (expression is LiteralExpressionSyntax literalExpression)
                    {
                        var value = literalExpression.Token.Value?.ToString();
                        if (IsSqlQuery(value))
                        {
                            AnalyzeSqlQuery(value, variables);
                        }
                    }
                }
                else
                {
                    Console.WriteLine($" - Unknown statement type: {statement.Kind()}");
                }
            }
        }
        else
        {
            Console.WriteLine("Method has no body.");
        }

        // Mapeamento de Estilo de Codificação: Verificação de métodos muito longos
        if (body != null && body.Statements.Count > 20)
        {
            Console.WriteLine($"        -> Method '{methodName}' is too long, consider refactoring.");
        }
    }

    private void AnalyzeInvocation(InvocationExpressionSyntax invocation, Dictionary<string, string> variables)
    {
        var methodCalled = invocation.Expression.ToString();

        // Mapeamento de Dependências Externas: Detecção de uso de bibliotecas HTTP e SQL
        if (methodCalled.Contains("HttpClient"))
        {
            Console.WriteLine("   -> External dependency detected: HttpClient (API call)");
        }
        if (methodCalled.Contains("SqlCommand"))
        {
            Console.WriteLine("   -> External dependency detected: SQL command");
        }

        Console.WriteLine($"   - Method call detected: {methodCalled}");

        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            Console.WriteLine($"      -> Argument: {argument}");
            if (variables.ContainsKey(argument.ToString()))
            {
                Console.WriteLine($"         (Uses variable: {argument})");
            }
        }
    }

    private bool IsSqlQuery(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;

        var sqlKeywords = new[] { "SELECT", "INSERT", "UPDATE", "DELETE", "FROM", "WHERE", "INTO", "VALUES", "SET" };
        return sqlKeywords.Any(keyword => value.StartsWith(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private void AnalyzeSqlQuery(string query, Dictionary<string, string> variables)
    {
        Console.WriteLine($"   - SQL Query detected: {query}");

        // Classify query type (SELECT, INSERT, UPDATE, DELETE)
        if (query.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("   - Query Type: SELECT");
            var table = ExtractTableFromQuery(query, "FROM");
            Console.WriteLine($"      -> Table: {table}");
        }
        else if (query.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("   - Query Type: INSERT");
            var table = ExtractTableFromQuery(query, "INTO");
            Console.WriteLine($"      -> Table: {table}");
        }
        else if (query.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("   - Query Type: UPDATE");
            var table = ExtractTableFromQuery(query, "UPDATE");
            Console.WriteLine($"      -> Table: {table}");
        }
        else if (query.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("   - Query Type: DELETE");
            var table = ExtractTableFromQuery(query, "FROM");
            Console.WriteLine($"      -> Table: {table}");
        }

        foreach (var variable in variables)
        {
            if (query.Contains(variable.Key))
            {
                Console.WriteLine($"      -> Uses variable: {variable.Key} (value: {variable.Value})");
            }
        }
    }

    private string ExtractTableFromQuery(string query, string keyword)
    {
        var keywordIndex = query.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
        if (keywordIndex == -1) return "Unknown";

        var afterKeyword = query.Substring(keywordIndex + keyword.Length).Trim();
        var parts = afterKeyword.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : "Unknown";
    }

    // Additional helper methods for loops, conditions, and others...

    private void AnalyzeLoop(StatementSyntax loopStatement)
    {
        if (loopStatement is ForStatementSyntax forStatement)
        {
            Console.WriteLine($"   - 'for' loop with condition: {forStatement.Condition}");

            // Analyze the initialization, condition, and incrementors
            foreach (var init in forStatement.Initializers)
            {
                Console.WriteLine($"   - Initializes: {init}");
            }

            if (forStatement.Incrementors.Any())
            {
                Console.WriteLine("   - Incrementing: ");
                foreach (var incrementor in forStatement.Incrementors)
                {
                    Console.WriteLine($"      -> {incrementor}");
                }
            }
        }
        else if (loopStatement is WhileStatementSyntax whileStatement)
        {
            Console.WriteLine($"   - 'while' loop with condition: {whileStatement.Condition}");
        }
        else if (loopStatement is DoStatementSyntax doStatement)
        {
            Console.WriteLine($"   - 'do-while' loop with condition: {doStatement.Condition}");
        }
    }

    private void AnalyzeCondition(IfStatementSyntax ifStatement)
    {
        Console.WriteLine($"   - 'if' condition: {ifStatement.Condition}");

        // Analyze the true block
        if (ifStatement.Statement is BlockSyntax trueBlock)
        {
            Console.WriteLine("   - Executes if condition is true:");
            foreach (var statement in trueBlock.Statements)
            {
                Console.WriteLine($"      -> {statement}");
            }
        }

        // Check for an 'else' block
        if (ifStatement.Else != null)
        {
            Console.WriteLine("   - 'else' block detected.");
            if (ifStatement.Else.Statement is BlockSyntax falseBlock)
            {
                Console.WriteLine("   - Executes if condition is false:");
                foreach (var statement in falseBlock.Statements)
                {
                    Console.WriteLine($"      -> {statement}");
                }
            }
        }
    }


    private void AnalyzeTryCatch(TryStatementSyntax tryStatement)
    {
        Console.WriteLine("   - Try block detected");

        // Analyze the try block
        foreach (var statement in tryStatement.Block.Statements)
        {
            Console.WriteLine($"      -> {statement}");
        }

        // Analyze catch clauses
        foreach (var catchClause in tryStatement.Catches)
        {
            if (catchClause.Declaration != null)
            {
                // Safe access to Declaration.Type if it exists
                Console.WriteLine($"   - Catches exception of type: {catchClause.Declaration.Type}");
            }
            else
            {
                // Handles generic catch blocks (without specifying an exception type)
                Console.WriteLine("   - Catches all exceptions (no specific exception type)");
            }

            // Analyze the catch block statements
            if (catchClause.Block != null)
            {
                Console.WriteLine("   - Catch block statements:");
                foreach (var statement in catchClause.Block.Statements)
                {
                    Console.WriteLine($"      -> {statement}");
                }
            }
        }

        // Check for a finally block
        if (tryStatement.Finally != null)
        {
            Console.WriteLine("   - Finally block detected");
            foreach (var statement in tryStatement.Finally.Block.Statements)
            {
                Console.WriteLine($"      -> {statement}");
            }
        }
    }

    private void AnalyzeAssignment(AssignmentExpressionSyntax assignment, Dictionary<string, string> variables)
    {
        var left = assignment.Left.ToString();
        var right = assignment.Right.ToString();
        Console.WriteLine($"   - Assigns: {left} = {right}");

        // Track variable assignment
        if (!variables.ContainsKey(left))
        {
            variables.Add(left, right);
        }
        else
        {
            variables[left] = right;
        }

        // Detect if the right-hand side involves a binary expression (e.g., a + b)
        if (assignment.Right is BinaryExpressionSyntax binaryExpression)
        {
            AnalyzeBinaryExpression(binaryExpression, variables);
        }
    }


    private void AnalyzeBinaryExpression(BinaryExpressionSyntax binaryExpression, Dictionary<string, string> variables)
    {
        var left = binaryExpression.Left.ToString();
        var right = binaryExpression.Right.ToString();
        var operatorToken = binaryExpression.OperatorToken;

        Console.WriteLine($"   - Calculation: {left} {operatorToken} {right}");

        // Detect method calls in the left or right side of the binary expression
        if (binaryExpression.Left is InvocationExpressionSyntax leftInvocation)
        {
            Console.WriteLine($"      -> Left operand is a method call: {leftInvocation.Expression}");
        }
        if (binaryExpression.Right is InvocationExpressionSyntax rightInvocation)
        {
            Console.WriteLine($"      -> Right operand is a method call: {rightInvocation.Expression}");
        }

        // Track if the calculation involves a known variable
        if (variables.ContainsKey(left))
        {
            Console.WriteLine($"   - Variable {left} is involved in the calculation.");
        }
    }

    private void ListMethodDependencies(MethodDeclarationSyntax method)
    {
        var invocationExpressions = method.DescendantNodes().OfType<InvocationExpressionSyntax>();

        if (!invocationExpressions.Any())
        {
            Console.WriteLine("            |-- (No dependencies found)");
            return;
        }

        foreach (var invocation in invocationExpressions)
        {
            var expression = invocation.Expression;

            if (expression is MemberAccessExpressionSyntax memberAccess)
            {
                var className = memberAccess.Expression.ToString();
                var methodName = memberAccess.Name.Identifier.Text;
                Console.WriteLine($"            |-- Calls method: {className}.{methodName}()");
            }
            else if (expression is IdentifierNameSyntax identifier)
            {
                var methodName = identifier.Identifier.Text;
                Console.WriteLine($"            |-- Calls method: {methodName}()");
            }
        }
    }


    private void PrintDirectoryStructure(string path, int indentLevel)
    {
        string indent = new string(' ', indentLevel * 2);

        // Lê a lista de diretórios ignorados do App.config
        var ignoredDirectoriesConfig = ConfigurationManager.AppSettings["IgnoredDirectories"];
        var directoriesToIgnore = new HashSet<string>(ignoredDirectoriesConfig.Split(','), StringComparer.OrdinalIgnoreCase);

        // Exibe a estrutura de diretórios, ignorando os especificados
        foreach (var directory in Directory.GetDirectories(path))
        {
            string dirName = Path.GetFileName(directory);

            // Se o diretório estiver na lista de ignorados, pulamos
            if (directoriesToIgnore.Contains(dirName))
            {
                continue;
            }

            Console.WriteLine($"{indent}|-- {dirName}");
            PrintDirectoryStructure(directory, indentLevel + 1);
        }

        // Exibe os arquivos no diretório
        foreach (var file in Directory.GetFiles(path))
        {
            Console.WriteLine($"{indent}|-- {Path.GetFileName(file)}");
        }
    }

}

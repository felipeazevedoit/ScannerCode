using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

        var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

        foreach (var @class in classDeclarations)
        {
            Console.WriteLine($"    |-- Classe: {@class.Identifier.Text}");

            var classDescription = GenerateClassDescription(@class.Identifier.Text);
            Console.WriteLine($"        (Descrição da classe: {classDescription})");

            var methodDeclarations = @class.DescendantNodes().OfType<MethodDeclarationSyntax>();

            foreach (var method in methodDeclarations)
            {
                var returnType = method.ReturnType.ToString();
                var methodName = method.Identifier.Text;
                var parameters = method.ParameterList.Parameters
                    .Select(p => $"{p.Type} {p.Identifier}")
                    .ToArray();

                Console.WriteLine($"        |-- {returnType} {methodName}({string.Join(", ", parameters)})");

                var methodDescription = GenerateMethodDescription(methodName, method.ParameterList.Parameters);
                Console.WriteLine($"            (Descrição do método: {methodDescription})");

                AnalyzeMethodLogic(method, methodName);

                ListMethodDependencies(method);
            }
        }
    }

    private string GenerateClassDescription(string className)
    {
        if (className.ToLower().Contains("service"))
        {
            return "Gerencia operações relacionadas a serviços.";
        }
        if (className.ToLower().Contains("controller"))
        {
            return "Controla a lógica de entrada e saída de uma aplicação.";
        }
        if (className.ToLower().Contains("repository"))
        {
            return "Gerencia o acesso aos dados e repositórios de persistência.";
        }
        if (className.ToLower().Contains("healthcheck"))
        {
            return "Realiza verificações de integridade e estado do sistema.";
        }
        return "Classe com funcionalidades específicas dentro do projeto.";
    }

    private string GenerateMethodDescription(string methodName, SeparatedSyntaxList<ParameterSyntax> parameters)
    {
        if (methodName.ToLower().Contains("get"))
        {
            return "Obtém informações ou dados de uma fonte.";
        }
        if (methodName.ToLower().Contains("set"))
        {
            return "Define ou atualiza valores de parâmetros ou propriedades.";
        }
        if (methodName.ToLower().Contains("check"))
        {
            return "Verifica uma condição ou estado, retornando o resultado.";
        }
        if (methodName.ToLower().Contains("save"))
        {
            return "Salva informações ou dados no sistema.";
        }
        if (methodName.ToLower().Contains("update"))
        {
            return "Atualiza dados ou parâmetros existentes.";
        }
        if (methodName.ToLower().Contains("delete"))
        {
            return "Remove ou exclui dados de uma fonte.";
        }

        foreach (var param in parameters)
        {
            var paramType = param.Type.ToString().ToLower();

            if (paramType.Contains("order"))
            {
                return "Processa operações relacionadas a pedidos.";
            }
            if (paramType.Contains("customer"))
            {
                return "Gerencia ou processa dados de clientes.";
            }
            if (paramType.Contains("product"))
            {
                return "Manipula informações de produtos.";
            }
            if (paramType.Contains("invoice") || paramType.Contains("payment"))
            {
                return "Gerencia transações financeiras ou faturas.";
            }
        }

        return "Executa uma lógica específica de negócio.";
    }

    private void AnalyzeMethodLogic(MethodDeclarationSyntax method, string methodName)
    {
        Console.WriteLine($"\nAnalisando o método: {methodName}");

        var body = method.Body;
        if (body != null)
        {
            Console.WriteLine("Lógica do método: ");
            foreach (var statement in body.Statements)
            {
                if (statement is ForStatementSyntax)
                {
                    Console.WriteLine(" - Contém um loop 'for'");
                }
                else if (statement is WhileStatementSyntax)
                {
                    Console.WriteLine(" - Contém um loop 'while'");
                }
                else if (statement is IfStatementSyntax)
                {
                    Console.WriteLine(" - Contém uma condição 'if'");
                }
                else if (statement is ExpressionStatementSyntax expressionStatement)
                {
                    var expression = expressionStatement.Expression;
                    if (expression is InvocationExpressionSyntax invocation)
                    {
                        var methodCalled = invocation.Expression.ToString();
                        Console.WriteLine($" - Chama o método: {methodCalled}");
                    }
                }
                else
                {
                    Console.WriteLine($" - Instrução desconhecida: {statement.Kind()}");
                }
            }
        }
        else
        {
            Console.WriteLine("Método sem corpo.");
        }
    }

    private void ListMethodDependencies(MethodDeclarationSyntax method)
    {
        var invocationExpressions = method.DescendantNodes().OfType<InvocationExpressionSyntax>();

        if (!invocationExpressions.Any())
        {
            Console.WriteLine("            |-- (Sem dependências)");
            return;
        }

        foreach (var invocation in invocationExpressions)
        {
            var expression = invocation.Expression;

            if (expression is MemberAccessExpressionSyntax memberAccess)
            {
                var className = memberAccess.Expression.ToString();
                var methodName = memberAccess.Name.Identifier.Text;
                Console.WriteLine($"            |-- Chama método: {className}.{methodName}()");
            }
            else if (expression is IdentifierNameSyntax identifier)
            {
                var methodName = identifier.Identifier.Text;
                Console.WriteLine($"            |-- Chama método: {methodName}()");
            }
        }
    }

    private void PrintDirectoryStructure(string path, int indentLevel)
    {
        string indent = new string(' ', indentLevel * 2);

        foreach (var directory in Directory.GetDirectories(path))
        {
            string dirName = Path.GetFileName(directory);

            if (dirName.Equals("objects", StringComparison.OrdinalIgnoreCase) ||
                dirName.Equals("obj", StringComparison.OrdinalIgnoreCase)) continue;

            Console.WriteLine($"{indent}|-- {dirName}");
            PrintDirectoryStructure(directory, indentLevel + 1);
        }

        foreach (var file in Directory.GetFiles(path))
        {
            Console.WriteLine($"{indent}|-- {Path.GetFileName(file)}");
        }
    }
}

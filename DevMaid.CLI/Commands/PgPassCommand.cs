using System.CommandLine;

using DevMaid.CLI.CommandOptions;
using DevMaid.CLI.Services;
using DevMaid.Core.Interfaces;
using DevMaid.Core.Models;
using DevMaid.Core.Services;

namespace DevMaid.CLI.Commands;

/// <summary>
/// Comando <c>devmaid database pgpass</c>.
/// Subcomando de <see cref="DatabaseCommand"/> para gerenciar o arquivo pgpass.conf.
/// </summary>
public static class PgPassCommand
{
    /// <summary>
    /// Constrói o comando <c>pgpass</c> com os subcomandos <c>add</c>, <c>list</c> e <c>remove</c>.
    /// </summary>
    public static Command Build()
    {
        var command = new Command("pgpass", "Gerencia o arquivo pgpass.conf para autenticação PostgreSQL sem senha.")
        {
            BuildAddCommand(),
            BuildListCommand(),
            BuildRemoveCommand()
        };

        return command;
    }

    // =========================================================================
    // Subcomando: add
    // =========================================================================

    private static Command BuildAddCommand()
    {
        var addCommand = new Command("add", "Adiciona uma nova entrada de credencial PostgreSQL ao pgpass.conf.");

        var bancoArgumento = new Argument<string>("banco")
        {
            Description = "Nome do banco de dados (ou * para curinga)."
        };

        var passwordOption = new Option<string?>("--password", "-W")
        {
            Description = "Senha PostgreSQL. Se não fornecida, será solicitada interativamente."
        };

        var hostOption = new Option<string?>("--host", "-h")
        {
            Description = "Hostname ou endereço IP do servidor PostgreSQL."
        };

        var portOption = new Option<string?>("--port", "-p")
        {
            Description = "Porta TCP do servidor PostgreSQL."
        };

        var usernameOption = new Option<string?>("--username", "-U")
        {
            Description = "Nome de usuário PostgreSQL."
        };

        addCommand.Add(bancoArgumento);
        addCommand.Add(passwordOption);
        addCommand.Add(hostOption);
        addCommand.Add(portOption);
        addCommand.Add(usernameOption);

        addCommand.SetAction(parseResult =>
        {
            var options = new PgPassAddOptions
            {
                Database = parseResult.GetValue(bancoArgumento) ?? string.Empty,
                Password = parseResult.GetValue(passwordOption),
                Hostname = parseResult.GetValue(hostOption),
                Port = parseResult.GetValue(portOption),
                Username = parseResult.GetValue(usernameOption)
            };

            return ExecuteAdd(options);
        });

        return addCommand;
    }

    // =========================================================================
    // Subcomando: list
    // =========================================================================

    private static Command BuildListCommand()
    {
        var listCommand = new Command("list", "Lista todas as entradas do pgpass.conf com senhas mascaradas.");

        listCommand.SetAction(_ => ExecuteList());

        return listCommand;
    }

    // =========================================================================
    // Subcomando: remove
    // =========================================================================

    private static Command BuildRemoveCommand()
    {
        var removeCommand = new Command("remove", "Remove uma entrada específica do pgpass.conf.");

        var bancoArgumento = new Argument<string>("banco")
        {
            Description = "Nome do banco de dados da entrada a remover (ou * para curinga)."
        };

        var hostOption = new Option<string?>("--host", "-h")
        {
            Description = "Hostname da entrada a remover."
        };

        var portOption = new Option<string?>("--port", "-p")
        {
            Description = "Porta da entrada a remover."
        };

        var usernameOption = new Option<string?>("--username", "-U")
        {
            Description = "Usuário da entrada a remover."
        };

        removeCommand.Add(bancoArgumento);
        removeCommand.Add(hostOption);
        removeCommand.Add(portOption);
        removeCommand.Add(usernameOption);

        removeCommand.SetAction(parseResult =>
        {
            var options = new PgPassRemoveOptions
            {
                Database = parseResult.GetValue(bancoArgumento) ?? string.Empty,
                Hostname = parseResult.GetValue(hostOption),
                Port = parseResult.GetValue(portOption),
                Username = parseResult.GetValue(usernameOption)
            };

            return ExecuteRemove(options);
        });

        return removeCommand;
    }

    // =========================================================================
    // Lógica de execução — add
    // =========================================================================

    private static int ExecuteAdd(PgPassAddOptions options)
    {
        // Validação: banco obrigatório (RF-011, código saída 2)
        if (string.IsNullOrWhiteSpace(options.Database))
        {
            Console.Error.WriteLine("Erro: o argumento <banco> é obrigatório.");
            return 2;
        }

        // Validação de host (RF-011, código saída 2)
        var host = options.Hostname ?? "localhost";
        if (!SecurityUtils.IsValidHost(host))
        {
            Console.Error.WriteLine($"Erro: formato de host inválido: \"{host}\".");
            return 2;
        }

        // Validação de porta (RF-011, código saída 2)
        var port = options.Port ?? "5432";
        if (!SecurityUtils.IsValidPort(port))
        {
            Console.Error.WriteLine("Erro: porta deve ser um número entre 1 e 65535.");
            return 2;
        }

        var username = options.Username ?? "postgres";

        // Senha: prompt interativo se ausente (Constituição Artigo I)
        var password = options.Password;
        if (string.IsNullOrEmpty(password))
        {
            Console.Write("Senha: ");
            password = PostgresPasswordHandler.ReadPasswordInteractively();
            Console.WriteLine();
        }

        // Validação: senha não pode ser vazia após prompt (RF-011, código saída 2)
        if (string.IsNullOrEmpty(password))
        {
            Console.Error.WriteLine("Erro: a senha não pode ser vazia.");
            return 2;
        }

        var entry = new PgPassEntry
        {
            Hostname = host,
            Port = port,
            Database = options.Database,
            Username = username,
            Password = password
        };

        var service = ResolveService();
        var resultado = service.AddEntry(entry);

        if (resultado.Success)
        {
            Console.WriteLine(resultado.Message);
            return 0;
        }
        else
        {
            // RF-012/RF-013: erros de I/O/permissão → código saída 1
            Console.Error.WriteLine(resultado.Message);
            return 1;
        }
    }

    // =========================================================================
    // Lógica de execução — list
    // =========================================================================

    private static int ExecuteList()
    {
        var service = ResolveService();
        var entradas = service.ListEntries().ToList();

        if (entradas.Count == 0)
        {
            Console.WriteLine("Nenhuma entrada configurada em pgpass.conf.");
            return 0;
        }

        // Cabeçalho da tabela
        var hostWidth = Math.Max("HOSTNAME".Length, entradas.Max(e => e.Hostname.Length));
        var portWidth = Math.Max("PORTA".Length, entradas.Max(e => e.Port.Length));
        var dbWidth = Math.Max("BANCO".Length, entradas.Max(e => e.Database.Length));
        var userWidth = Math.Max("USUÁRIO".Length, entradas.Max(e => e.Username.Length));
        const int senhaWidth = 5; // "SENHA" — reservado para futura expansão de largura de coluna
        _ = senhaWidth;

        var cabecalho = $"{"HOSTNAME".PadRight(hostWidth)}  {"PORTA".PadRight(portWidth)}  {"BANCO".PadRight(dbWidth)}  {"USUÁRIO".PadRight(userWidth)}  SENHA";
        Console.WriteLine(cabecalho);
        Console.WriteLine(new string('-', cabecalho.Length));

        foreach (var entrada in entradas)
        {
            Console.WriteLine(
                $"{entrada.Hostname.PadRight(hostWidth)}  {entrada.Port.PadRight(portWidth)}  {entrada.Database.PadRight(dbWidth)}  {entrada.Username.PadRight(userWidth)}  ****");
        }

        return 0;
    }

    // =========================================================================
    // Lógica de execução — remove
    // =========================================================================

    private static int ExecuteRemove(PgPassRemoveOptions options)
    {
        // Validação: banco obrigatório
        if (string.IsNullOrWhiteSpace(options.Database))
        {
            Console.Error.WriteLine("Erro: o argumento <banco> é obrigatório.");
            return 2;
        }

        var chave = new PgPassEntry
        {
            Hostname = options.Hostname ?? "localhost",
            Port = options.Port ?? "5432",
            Database = options.Database,
            Username = options.Username ?? "postgres",
            Password = "placeholder" // Não usado na comparação — chave de identidade não inclui senha
        };

        var service = ResolveService();
        var resultado = service.RemoveEntry(chave);

        if (resultado.Success)
        {
            Console.WriteLine(resultado.Message);
            return 0;
        }
        else if (resultado.Message.StartsWith("Entrada não encontrada"))
        {
            // Entrada não encontrada é informativo — código saída 0
            Console.WriteLine(resultado.Message);
            return 0;
        }
        else
        {
            // Erros de I/O/permissão → código saída 1
            Console.Error.WriteLine(resultado.Message);
            return 1;
        }
    }

    // =========================================================================
    // Resolução do serviço
    // =========================================================================

    private static IPgPassService ResolveService()
    {
        return new PgPassService();
    }
}

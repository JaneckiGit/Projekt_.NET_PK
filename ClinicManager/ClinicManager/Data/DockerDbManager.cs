using System.Diagnostics;
using System.Net.Sockets;

namespace ClinicManager.Data;

public static class DockerDbManager
{
    private const string ContainerName = "sql_server_clinic";
    private const int Port = 1433;

    public static void EnsureSqlServerContainerRunning()
    {
        Console.WriteLine("[DockerDbManager] Checking database environment...");
        
        try
        {
            var (code, output, _) = RunCommand("docker", "--version");
            if (code != 0)
            {
                Console.WriteLine("[DockerDbManager] Warning: Docker command execution returned non-zero code. Docker might not be running. Skipping container check.");
                return;
            }
            Console.WriteLine($"[DockerDbManager] Docker detected: {output}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DockerDbManager] Warning: Failed to execute docker command ({ex.Message}). Docker Desktop might not be installed or running. Skipping automatic startup.");
            return;
        }
        
        var (inspectCode, inspectOutput, inspectError) = RunCommand("docker", $"inspect -f \"{{{{.State.Running}}}}\" {ContainerName}");

        if (inspectCode == 0)
        {
            if (inspectOutput.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[DockerDbManager] Database container '{ContainerName}' is already running.");
                return;
            }
            else
            {
                Console.WriteLine($"[DockerDbManager] Database container '{ContainerName}' is stopped. Starting container...");
                var (startCode, _, startError) = RunCommand("docker", $"start {ContainerName}");
                if (startCode != 0)
                {
                    Console.WriteLine($"[DockerDbManager] Error: Failed to start database container: {startError}");
                    return;
                }
                Console.WriteLine($"[DockerDbManager] Database container '{ContainerName}' started successfully.");
                WaitForPort(Port);
            }
        }
        else
        {
            Console.WriteLine($"[DockerDbManager] Database container '{ContainerName}' does not exist. Creating and starting a new one...");
            
            var runArgs = $"run -e \"ACCEPT_EULA=Y\" -e \"MSSQL_SA_PASSWORD=ClinicPass123!\" -p {Port}:{Port} --name {ContainerName} -d mcr.microsoft.com/mssql/server:2022-latest";
            
            var (runCode, runOutput, runError) = RunCommand("docker", runArgs);
            if (runCode != 0)
            {
                Console.WriteLine($"[DockerDbManager] Error: Failed to create database container: {runError}");
                return;
            }
            
            Console.WriteLine($"[DockerDbManager] Database container created successfully (ID: {runOutput.Substring(0, Math.Min(12, runOutput.Length))}).");
            WaitForPort(Port);
        }
    }

    private static (int ExitCode, string Output, string Error) RunCommand(string fileName, string arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd().Trim();
        string error = process.StandardError.ReadToEnd().Trim();
        process.WaitForExit();

        return (process.ExitCode, output, error);
    }

    private static void WaitForPort(int port)
    {
        Console.WriteLine($"[DockerDbManager] Waiting for SQL Server to be ready on port {port}...");
        
        int attempt = 1;
        const int maxAttempts = 20;

        while (attempt <= maxAttempts)
        {
            try
            {
                using var client = new TcpClient();
                client.Connect("127.0.0.1", port);
                Console.WriteLine("[DockerDbManager] Port is open! Giving SQL Server 5 seconds to complete startup...");
                Thread.Sleep(5000);
                Console.WriteLine("[DockerDbManager] SQL Server is ready to receive network connections!");
                return;
            }
            catch
            {
                Console.WriteLine($"[DockerDbManager] [Attempt {attempt}/{maxAttempts}] Port {port} is not open yet. Waiting 1 second...");
                attempt++;
                Thread.Sleep(1000);
            }
        }

        Console.WriteLine("[DockerDbManager] Warning: Reached timeout waiting for database port to open. Proceeding anyway...");
    }
}

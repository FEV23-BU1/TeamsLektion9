using System.Text.Json;

namespace SkolSystem;

public interface IMessageParser
{
    bool Parse(string json);
}

public abstract class Command
{
    public abstract void Execute();
}

public enum Action
{
    Login = 0,
    Register = 1,
}

public class LoginCommand
{
    public string Email { get; set; }
    public string Password { get; set; }

    public LoginCommand(string email, string password)
    {
        this.Email = email;
        this.Password = password;
    }
}

public class RegisterCommand : Command
{
    public string Content { get; set; }

    public RegisterCommand(string content)
    {
        this.Content = content;
    }

    public override void Execute()
    {
        Console.WriteLine($"Content {Content}");
    }
}

public class RegisterParser : IMessageParser
{
    public bool Parse(string json)
    {
        RegisterCommand? command = JsonSerializer.Deserialize<RegisterCommand>(json);
        if (command == null)
        {
            return false;
        }

        command.Execute();
        return true;
    }
}

using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace SkolSystem;


/*public class Book
{
    public string Title { get; set; }
    public string Author { get; set; }
}

public class ClientProgram
{
    public static void Start()
    {
        IPAddress ipAddress = new IPAddress(new byte[] { 127, 0, 0, 1 });
        IPEndPoint ipEndpoint = new IPEndPoint(ipAddress, 25500);

        // Vi skapar en socket för att kunna ansluta till servern.
        Socket clientSocket = new Socket(
            ipAddress.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp
        );

        // Vi försöker ansluta till servern.
        clientSocket.Connect(ipEndpoint);
        Console.WriteLine("We have connected to the server!");

        // Om vi lyckas ansluta till servern så måste man logga in först.
        // Det gör vi här från rad 32 till rad 49
        Console.WriteLine("Please enter your email:");
        string? email = Console.ReadLine();
        if (email == null)
        {
            return;
        }
        Console.WriteLine("Please enter your password:");
        string? password = Console.ReadLine();
        if (password == null)
        {
            return;
        }

        string json = JsonSerializer.Serialize(new LoginCommand(email, password));

        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(json);
        clientSocket.Send(buffer);

        // Sedan skall vi kunna skriva kommandon till servern hur många gånger vi vill,
        // så vi kör på en while loop.
        while (true)
        {
            string? input = Console.ReadLine();
            if (input == null)
            {
                return;
            }

            if (input == "register")
            {
                Console.WriteLine("Please enter some content:");
                string content = Console.ReadLine() ?? "";
                json = "1" + JsonSerializer.Serialize(new RegisterCommand(content));
            }
            else
            {
                continue;
            }

            buffer = System.Text.Encoding.UTF8.GetBytes(json);
            clientSocket.Send(buffer);
        }
    }
}
*/

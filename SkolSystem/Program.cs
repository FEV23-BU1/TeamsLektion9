using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using MongoDB.Driver.Core.Events;

namespace SkolSystem;

/*

1. Skapa användare
   - Endast som `Admin`
2. Logga in som användare
3. Logga ut som användare
4. Skapa klasser och koppla användare
   - Som `Admin` eller `Lärare`
5. Skapa kurser och koppla till klasser
   - Som `Admin` eller `Lärare`
6. Lägga in betyg för klass, studerande och kurs
   - Som `Admin` eller `Lärare`
7. Se betyg som användare
   - Egna betyg om man är inloggad som `Studerande`
   - Alla betyg om man är inloggad som `Admin` eller `Lärare`
8. Meddela andra användare
   - Med två olika system: email & internt

Användartyper:
- Admin
   - Det finns endast ett Admin konto
- Lärare
- Studerande

*/

class Program
{
    static void Main(string[] args)
    {
        //if (true)
        //{
        //    ClientProgram.Start();
        //    return;
        //}

        IdGenerator idGenerator = new NullIdGenerator();
        SchoolApplication app = new SchoolApplication(
            idGenerator,
            new LocalUserService(idGenerator, new DbUserRepository("schoolsystem")),
            new LocalGroupService(),
            new LocalCourseService()
        );

        app.Start();
    }
}

// Vi måste på något sätt spara all information som finns i hela programmet. Det är vad denna klass är till för.
// Den håller all information (alla användare, alla kurser, alla klasser, alla betyg och så vidare).
public class SchoolApplication
{
    // Det skall endast finnas en admin i hela programmet och därför lägger vi den som variabel här.
    // Det blir enkelt att komma åt den då.
    // TODO: Vi vill dock att den ska ligga i "UserService" eftersom den tillhör det systemet.
    private Administrator _admin;

    // Vi måste på något sätt hantera och spara användare. Det är vad "IUserService" är till för.
    // Vi har alltså en egen struktur för att spara och hantera användare.
    // Anledningen till att det är en egen struktur (och ett interface) är för att vi enkelt
    // kan bygga ut det då. Vi kan kan byta mellan databaser, filer, test saker och annat.
    // Är en Singleton.
    private IUserService _userService;

    // Vi måste på något sätt hantera och spara klasser (grupper). Det är vad "IGroupService" är till för.
    // Vi har alltså en egen struktur för att spara och hantera klasser (grupper).
    // Anledningen till att det är en egen struktur (och ett interface) är för att vi enkelt
    // kan bygga ut det då. Vi kan kan byta mellan databaser, filer, test saker och annat.
    // Är en Singleton.
    private IGroupService _groupService;

    // Vi måste på något sätt hantera och spara kurser. Det är vad "ICourseService" är till för.
    // Vi har alltså en egen struktur för att spara och hantera kurser.
    // Anledningen till att det är en egen struktur (och ett interface) är för att vi enkelt
    // kan bygga ut det då. Vi kan kan byta mellan databaser, filer, test saker och annat.
    // Är en Singleton.
    private ICourseService _courseService;

    // Denna lista håller koll på alla klienter som har anslutit OCH loggat in.
    private List<Connection> connections;

    // Denna dictionary håller koll på alla kommandon vi har. Den mappar/kopplar ett tecken (1, 2, 3) till ett kommando.
    private Dictionary<char, IMessageParser> parsers;

    // En constructor för att enkelt kunna lägga in värden för alla services.
    public SchoolApplication(
        IdGenerator idGenerator,
        IUserService userService,
        IGroupService groupService,
        ICourseService courseService
    )
    {
        // Hårdkoda admin användaren eftersom den endast skall finnas en instans.
        // Detta kallas för "Singleton" eftersom det endast finns en instans.
        this._admin = new Administrator(idGenerator);
        this.connections = new List<Connection>();
        this.parsers = new Dictionary<char, IMessageParser>();
        this._userService = userService;
        this._groupService = groupService;
        this._courseService = courseService;

        // Här lägger vi in alla kommandon som vi vill kunna köra.
        // Just nu kan man bara registrera.
        parsers.Add('1', new RegisterParser());
    }

    // Metoden som skall starta igång hela programmet.
    public void Start()
    {
        // localhost = 127.0.0.1
        IPAddress ipAddress = new IPAddress(new byte[] { 127, 0, 0, 1 });
        IPEndPoint ipEndpoint = new IPEndPoint(ipAddress, 25500);

        // Vi skapar en socket för servern.
        Socket serverSocket = new Socket(
            ipAddress.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp
        );

        // Här säger vi att det skall vara en server socket och börjar lyssna efter anslutningar.
        serverSocket.Bind(ipEndpoint);
        serverSocket.Listen();

        // Denna lista håller koll på alla som försöker logga in.
        List<Socket> pendingConnections = new List<Socket>();

        // Sockets skickar all data i binärt, så vi har en generell array för att spara alla meddelanden som skickas til servern.
        byte[] buffer = new byte[1024];

        // Vi vill att hur många klienter som helst skall kunna ansluta, så vi loopar oändligt.
        while (true)
        {
            // Vi vill inte blocka koden om ingen försöker ansluta, så vi kör en "poll" för att se om det är någon som vill ansluta.
            // 10 betyder att vi väntar max 10 microsekunder.
            if (serverSocket.Poll(10, SelectMode.SelectRead))
            {
                // Accept hämtar ut nya anslutningar.
                Socket clientSocket = serverSocket.Accept();
                pendingConnections.Add(clientSocket);
                Console.WriteLine("A client has connected!");
            }

            // Vi loopar igenom alla som försöker logga in.
            for (int i = 0; i < pendingConnections.Count; i++)
            {
                Socket pending = pendingConnections[i];
                // Samma sak här som för "accept". Vi vill inte blocka koden om klienten inte har skickat ett meddelande.
                if (pending.Available == 0)
                {
                    continue;
                }

                // Vi läser meddelandet och försöker logga in. (från rad 162 till 183)
                int read = pending.Receive(buffer);
                string message = System.Text.Encoding.UTF8.GetString(buffer, 0, read);
                LoginCommand? command = JsonSerializer.Deserialize<LoginCommand>(message);

                if (command == null)
                {
                    Console.WriteLine("Login parsing failed: " + message);
                    return;
                }

                // Vi ser om det finns en användare i databasen med rätt email och lösenord.
                User? user = this._userService.Login(command.Email, command.Password);
                if (user == null)
                {
                    // TODO: Send message to client
                    Console.WriteLine("Login failed: " + message);
                    continue;
                }

                Connection connection = new Connection();
                connection.Socket = pending;
                connection.User = user;
                this.connections.Add(connection);

                // Eftersom inloggningen lyckades, så flyttar vi anslutningen till "connections" och tar bort från "pendingConnections".
                pendingConnections.RemoveAt(i);
                i--;
                Console.WriteLine($"User {user.Email} has logged in!");
            }

            // Vi loopar igenom alla som har loggat in och ser vad dem vill göra (vilka kommandon de vill köra).
            for (int i = 0; i < this.connections.Count; i++)
            {
                Connection connection = connections[i];
                // Samma sak här som för "accept". Vi vill inte blocka koden om klienten inte har skickat ett meddelande.
                if (connection.Socket.Available == 0)
                {
                    continue;
                }

                // Vi läser in meddelandet från klienten och gör om det till en sträng (vilket är i json).
                int read = connection.Socket.Receive(buffer);
                string message = System.Text.Encoding.UTF8.GetString(buffer, 0, read);
                char action = message[0];

                // Vi försöker hämta ut kommandon som klienten försöker köra.
                IMessageParser? parser = this.parsers[action];
                if (parser == null)
                {
                    Console.WriteLine("Could not parse command.");
                    continue;
                }

                // Koden här försöker parsa json objektet och sedan köra kommandot.
                if (!parser.Parse(message.Substring(1)))
                {
                    Console.WriteLine("Could not parse command.");
                }
            }
        }
    }
}

/*this._userService.Register(
            "Adam",
            "Olofsson",
            "adam@olofsson.se",
            "pass123",
            UserType.Student
        );
        this._userService.Register("Ironman", "", "ironman@stark.com", "pass123", UserType.Teacher);
        this._userService.Register(
            "Fredrik",
            "Andersson",
            "fredrik@andersson.se",
            "pass123",
            UserType.Student
        );

        User? loggedIn = this._userService.Login("adam@olofsson.se", "pass123");
        if (loggedIn != null)
        {
            Console.WriteLine(loggedIn.FirstName);
        }*/

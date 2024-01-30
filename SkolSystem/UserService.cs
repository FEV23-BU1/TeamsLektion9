using MongoDB.Driver;

namespace SkolSystem;

// Vi skapar ett interface för användarsystemet som lägger upp vad som skall
// kunna vara möjligt att göra.
// Med andra ord: man skall kunna registrera användare, logga in och ut.
// Vi lägger dock inte in en specifik implementation (därvav interface) eftersom vi vill
// kunna ändra på beteendet om vi vill.
// Med ett interface kan vi enkelt ha olika versioner, såsom "DatabaseUserService", "LocalUserService" och "TestUserService".
public interface IUserService
{
    User Register(string firstName, string lastName, string email, string password, UserType type);
    User? Login(string email, string password);
    void Logout();
}

// Vi bryter ut datalagringen och hanteringen till ett eget interface. För då kan vi enkelt styra hur vi vill spara informationen.
// Vi kan exempelvis ha ett repository för MongoDB och ett för lokala listor.
public interface IUserRepository
{
    void Save(User user);
    User? GetUserByEmailAndPassword(string email, string password);
    List<User> GetAll();
    User? GetUserByEmail(string email);
}

// En version/implementation av interfacet som sparar alla användare i en lokal lista.
// Den skall användas av det "riktiga" programmet.
public class LocalUserService : IUserService
{
    private IdGenerator idGenerator;

    // Vi använder nu interfacet istället för en lista direkt så att vi kan byta ut mellan olika implementationer enkelt.
    private IUserRepository users;
    private User? loggedIn;

    public LocalUserService(IdGenerator idGenerator, IUserRepository repository)
    {
        this.users = repository;
        this.idGenerator = idGenerator;
        this.loggedIn = null;
    }

    public User Register(
        string firstName,
        string lastName,
        string email,
        string password,
        UserType type
    )
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("First name cannot be null or empty");
        }
        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("Last name cannot be null or empty");
        }
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be null or empty");
        }
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be null or empty");
        }

        User? existing = this.users.GetUserByEmail(email);
        if (existing != null)
        {
            throw new ArgumentException("Email already used");
        }

        User user;
        if (type == UserType.Teacher)
        {
            user = new Teacher(idGenerator, firstName, lastName, email, password);
        }
        else if (type == UserType.Student)
        {
            user = new Student(idGenerator, firstName, lastName, email, password);
        }
        else
        {
            throw new ArgumentException("No such type exists");
        }

        this.users.Save(user);
        return user;
    }

    public User? Login(string email, string password)
    {
        // Vi letar efter en användare som matchar med "email" och "password".
        User? user = this.users.GetUserByEmailAndPassword(email, password);

        if (user == null)
        {
            return null;
        }

        this.loggedIn = user;
        return user;
    }

    public void Logout()
    {
        this.loggedIn = null;
    }
}

// En implementation av IUserRepository som använder MongoDB för att spara och hantera data.
public class DbUserRepository : IUserRepository
{
    MongoClient dbClient;
    IMongoDatabase db;
    IMongoCollection<User> collection;

    public DbUserRepository(string database)
    {
        // Vi börjar med att ansluta till databasen (i docker) och kopplar till en valfri databas (schoolsystem) och collection (users).
        this.dbClient = new MongoClient("mongodb://localhost:27017/" + database);
        this.db = dbClient.GetDatabase(database);
        this.collection = db.GetCollection<User>("users");
    }

    public void Clear()
    {
        this.db.DropCollection("users");
    }

    public List<User> GetAll()
    {
        throw new NotImplementedException();
    }

    // Denna metod hämtar en användare med en specifik email och lösenord.
    public User? GetUserByEmailAndPassword(string email, string password)
    {
        // TODO: Fixa felet som uppstår när det inte finns en passande användare.
        var filter = Builders<User>.Filter.Where(u => u.Email == email && u.Password == password);
        // "Find" kan returnera flera objekt, så vi använder ".First()" för att hämta ut den första den hittade.
        var result = this.collection.Find(filter);
        if (result.CountDocuments() == 0)
        {
            return null;
        }

        return result.First();
    }

    // Denna metod sparar en användare till databasen.
    public void Save(User user)
    {
        this.collection.InsertOne(user);
    }

    public User? GetUserByEmail(string email)
    {
        var filter = Builders<User>.Filter.Where(u => u.Email == email);
        var result = this.collection.Find(filter);
        if (result.CountDocuments() == 0)
        {
            return null;
        }

        return result.First();
    }
}

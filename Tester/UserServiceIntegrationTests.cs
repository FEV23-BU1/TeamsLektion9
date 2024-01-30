using SkolSystem;

namespace Tester;

public class UserServiceIntegrationTests
{
    [Fact]
    public void Test1()
    {
        string firstName = "Tony";
        string lastName = "Stark";
        string email = "tony@stark.com";
        string password = "ironman";
        DbUserRepository repository = new DbUserRepository("schoolsystemtest");
        IUserService service = new LocalUserService(new NullIdGenerator(), repository);
        repository.Clear();

        User user = service.Register(firstName, lastName, email, password, UserType.Student);
        Assert.Equal(firstName, user.FirstName);
        Assert.Equal(lastName, user.LastName);
        Assert.Equal(email, user.Email);
        Assert.Equal(password, user.Password);
        Assert.True(user is Student);
        Assert.False(user is Teacher);

        User? login = service.Login(email, password);
        Assert.True(login != null);
        Assert.Equal(firstName, login.FirstName);
        Assert.Equal(lastName, login.LastName);
        Assert.Equal(email, login.Email);
        Assert.Equal(password, login.Password);
        Assert.True(login is Student);
        Assert.False(login is Teacher);

        Assert.Throws<ArgumentException>(() =>
        {
            User user = service.Register(firstName, lastName, email, password, UserType.Student);
        });
    }

    [Theory]
    [InlineData("", "Banner", "bruce@banner.com", "hulk")]
    [InlineData("Bruce", "", "bruce@banner.com", "hulk")]
    [InlineData("Bruce", "Banner", "", "hulk")]
    [InlineData("Bruce", "Banner", "bruce@banner.com", "")]
    [InlineData("", "", "bruce@banner.com", "hulk")]
    public void BadInputs(string firstName, string lastName, string email, string password)
    {
        IUserService service = new LocalUserService(
            new NullIdGenerator(),
            new DbUserRepository("schoolsystemtest")
        );

        Assert.Throws<ArgumentException>(() =>
        {
            User user = service.Register(firstName, lastName, email, password, UserType.Student);
        });

        User? login = service.Login(email, password);
        Assert.True(login == null);
    }
}

using Moq;
using SkolSystem;

namespace Tester;

public class UserServiceUnitTests
{
    [Fact]
    public void Test1()
    {
        string firstName = "Tony";
        string lastName = "Stark";
        string email = "tony@stark.com";
        string password = "ironman";

        var mock = new Moq.Mock<IUserRepository>();

        IUserService service = new LocalUserService(new NullIdGenerator(), mock.Object);

        User user = service.Register(firstName, lastName, email, password, UserType.Student);
        Assert.Equal(firstName, user.FirstName);
        Assert.Equal(lastName, user.LastName);
        Assert.Equal(email, user.Email);
        Assert.Equal(password, user.Password);
        Assert.True(user is Student);
        Assert.False(user is Teacher);
    }

    [Theory]
    [InlineData("", "Banner", "bruce@banner.com", "hulk")]
    [InlineData("Bruce", "", "bruce@banner.com", "hulk")]
    [InlineData("Bruce", "Banner", "", "hulk")]
    [InlineData("Bruce", "Banner", "bruce@banner.com", "")]
    [InlineData("", "", "bruce@banner.com", "hulk")]
    public void BadInputs(string firstName, string lastName, string email, string password)
    {
        var mock = new Moq.Mock<IUserRepository>();
        IUserService service = new LocalUserService(new NullIdGenerator(), mock.Object);

        Assert.Throws<ArgumentException>(() =>
        {
            User user = service.Register(firstName, lastName, email, password, UserType.Student);
        });
    }

    [Fact]
    public void LoginSuccess()
    {
        string email = "tony@stark.com";
        string password = "ironman";

        var mock = new Moq.Mock<IUserRepository>();
        mock.Setup(repo => repo.GetUserByEmailAndPassword(email, password))
            .Returns(new User(new NullIdGenerator(), "", "", email, password));

        IUserService service = new LocalUserService(new NullIdGenerator(), mock.Object);

        User? user = service.Login(email, password);
        Assert.True(user != null);
    }

    [Fact]
    public void LoginFail()
    {
        string email = "tony@stark.com";
        string password = "ironman";

        var mock = new Moq.Mock<IUserRepository>();
        mock.Setup(repo => repo.GetUserByEmailAndPassword(email, password)).Returns<User?>(null);

        IUserService service = new LocalUserService(new NullIdGenerator(), mock.Object);

        User? user = service.Login(email, password);
        Assert.True(user == null);
    }
}

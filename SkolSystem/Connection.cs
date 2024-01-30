using System.Net.Sockets;

namespace SkolSystem;

public class Connection
{
    public Socket Socket { get; set; }

    public User User { get; set; }
}

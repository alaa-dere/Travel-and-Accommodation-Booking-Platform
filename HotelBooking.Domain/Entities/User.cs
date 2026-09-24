namespace HotelBooking.Domain.Entities;

public class User
{
    public int UserId { get; set; }
    public string FirstName { get; set; } 
    public string LastName { get; set; }  
    public string Username { get; set; } 
    public string Email { get; set; }  
    public string PasswordHash { get; set; } 
    public Role Role { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public User(string firstName,string lastName, string username, string email, string passwordHash, Role role)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("FirstName is required",nameof(firstName));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("LastName is required",nameof(lastName));
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username is required",nameof(username));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required",nameof(email));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("PasswordHash is required",nameof(passwordHash));
        }
        
        FirstName = firstName;
        LastName = lastName;
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        CreatedAt = DateTime.UtcNow;
    }
}

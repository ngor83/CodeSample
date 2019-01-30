namespace BBWT.Services.Classes
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    using BBWT.Data.Membership;
    using BBWT.Data.Security;
    using BBWT.Domain;
    using BBWT.Services.Interfaces;

    /// <summary>
    /// Security service implementation
    /// </summary>
    public class SecurityService : ISecurityService
    {
        public static readonly int TicketExpiredIn = 10;

        private readonly IDataContext context;

        /// <summary>
        /// Constructs SecurityService instance
        /// </summary>
        /// <param name="ctx">Data context</param>
        public SecurityService(IDataContext ctx)
        {
            this.context = ctx;
        }

        /// <summary>
        /// Create basic security ticket
        /// </summary>
        /// <returns>generated ticket</returns>
        public string CreateTicket()
        {
            string ticket;
            do
            {
                ticket = Guid.NewGuid().ToString();
            }
            while (this.context.SecurityTickets.Any(entry => entry.Ticket == ticket));

            this.context.SecurityTickets.Add(new SecurityTicket
            {
                Ticket = ticket,
                ExpiredOn = DateTimeOffset.UtcNow.AddDays(TicketExpiredIn)
            });

            this.context.Commit();
            return ticket;
        }

        /// <summary>
        /// Create ticket for user
        /// </summary>
        /// <param name="user">User to parametrise ticket</param>
        /// <returns>Generated ticket</returns>
        public string CreateTicketForUser(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user", "User should not be null");
            }

            string ticket;
            do
            {
                ticket = Guid.NewGuid().ToString();
            }
            while (this.context.UserSecurityTickets.Any(entry => entry.Ticket == ticket));

            this.context.UserSecurityTickets.Add(new UserSecurityTicket
            {
                Ticket = ticket,
                ExpiredOn = DateTimeOffset.UtcNow.AddDays(TicketExpiredIn),
                User = user
            });

            this.context.Commit();
            return ticket;
        }
        
        /// <summary>Get encoded url for ticket</summary>
        /// <param name="url">url</param>
        /// <param name="ticket">ticket</param>
        /// <returns>encoded url</returns>
        public string EncodeTicket(string url, Guid ticket)
        {            
            byte[] bytesToEncode = Encoding.Unicode.GetBytes(ticket.ToString());
            string encodedTicket = Convert.ToBase64String(bytesToEncode);

            string link = string.Format("{0}?ticket={1}", url, encodedTicket);
            return link;
        }

        /// <summary>Decode ticket</summary>
        /// <param name="encodedTicket">encoded ticket</param>
        /// <returns>ticket</returns>
        public string DecodeTicket(string encodedTicket)
        {
            string ticket;
            if ((encodedTicket.Length % 4 == 0) &&
                Regex.IsMatch(encodedTicket, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None))
            {
                byte[] decodedBytes = Convert.FromBase64String(encodedTicket);
                ticket = Encoding.Unicode.GetString(decodedBytes);
            }
            else
            {
                throw new ArgumentException("Decoded ticket is not a valid", "ticket");
            }

            return ticket;
        }

        /// <summary>Get user information by ticket</summary>
        /// <param name="ticket">ticket</param>
        /// <returns>user</returns>
        public User GetUserByTicket(string ticket)
        {
            Guid ticketGuid;
            if (!Guid.TryParse(ticket, out ticketGuid))
            {
                throw new ArgumentException("Ticket is not a valid Guid", "ticket");
            }

            var securedTicket = this.context.UserSecurityTickets.FirstOrDefault(entry => entry.Ticket == ticket && entry.ExpiredOn >= DateTimeOffset.UtcNow && !entry.IsUsed);
            return securedTicket != null ? securedTicket.User : null;            
        }

        /// <summary>
        /// Create ticket for company registration
        /// </summary>
        /// <param name="company">Company</param>
        /// <param name="group">Group</param>
        /// <param name="email">Email</param>
        /// <param name="firstName">First Name</param>
        /// <param name="secondName">Second Name</param>
        /// <returns>Generated ticket</returns>
        public string CreateTicketForCompany(Company company, BBWT.Data.Membership.Group group, string email = null, string firstName = null, string secondName = null)
        {
            if (company == null && group == null)
            {
                throw new ArgumentException("No company or group ID selected");
            }

            string ticket;
            do
            {
                ticket = Guid.NewGuid().ToString();
            }
            while (this.context.CompanySecurityTickets.Any(entry => entry.Ticket == ticket));

            this.context.CompanySecurityTickets.Add(new CompanySecurityTicket
            {
                Ticket = ticket,
                ExpiredOn = DateTimeOffset.UtcNow.AddDays(TicketExpiredIn),
                Company = company,
                Group = group,
                Email = email,
                FirstName = firstName,
                SecondName = secondName
            });

            this.context.Commit();
            return ticket;
        }

        /// <summary>
        /// Check if basic ticket is valid
        /// </summary>
        /// <param name="ticket">ticket value</param>
        /// <returns>ticket</returns>
        public SecurityTicket CheckTicket(string ticket)
        {
            Guid ticketGuid;
            if (!Guid.TryParse(ticket, out ticketGuid))
            {
                throw new ArgumentException("Ticket is not a valid Guid", "ticket");
            }

            return this.context.SecurityTickets.FirstOrDefault(entry => entry.Ticket == ticket && entry.ExpiredOn >= DateTimeOffset.UtcNow && !entry.IsUsed);
        }

        /// <summary>
        /// Check user security ticket
        /// </summary>
        /// <param name="ticket">ticket value</param>
        /// <param name="user">user</param>
        /// <returns>ticket</returns>
        public UserSecurityTicket CheckTicketForUser(string ticket, User user)
        {
            Guid ticketGuid;
            if (!Guid.TryParse(ticket, out ticketGuid))
            {
                throw new ArgumentException("Ticket is not a valid Guid", "ticket");
            }

            return this.context.UserSecurityTickets.FirstOrDefault(entry => entry.Ticket == ticket && entry.User == user
                && entry.ExpiredOn >= DateTimeOffset.UtcNow && !entry.IsUsed);
        }

        /// <summary>
        /// Check company security ticket
        /// </summary>
        /// <param name="ticket">ticket value</param>
        /// <param name="company">company</param>
        /// <returns>ticket</returns>
        public CompanySecurityTicket CheckTicketForCompany(string ticket, Company company)
        {
            Guid ticketGuid;
            if (!Guid.TryParse(ticket, out ticketGuid))
            {
                throw new ArgumentException("Ticket is not a valid Guid", "ticket");
            }

            if (company == null)
            {
                throw new ArgumentNullException("company", "Company should not be null");
            }

            return this.context.CompanySecurityTickets.FirstOrDefault(entry => entry.Ticket == ticket &&
                entry.Company == company && entry.ExpiredOn >= DateTimeOffset.UtcNow && !entry.IsUsed);
        }

        /// <summary>
        /// Check company group
        /// </summary>
        /// <param name="ticket">ticket value</param>
        /// <param name="group">group</param>
        /// <returns>company ticket</returns>
        public CompanySecurityTicket CheckTicketForCompanyGroup(string ticket, BBWT.Data.Membership.Group group)
        {
            Guid ticketGuid;
            if (!Guid.TryParse(ticket, out ticketGuid))
            {
                throw new ArgumentException("Ticket is not a valid Guid", "ticket");
            }

            if (group == null)
            {
                throw new ArgumentNullException("group", "Group should not be null");
            }

            return this.context.CompanySecurityTickets.FirstOrDefault(entry => entry.Ticket == ticket &&
                entry.Group == group && entry.ExpiredOn >= DateTimeOffset.UtcNow && !entry.IsUsed);
        }

        /// <summary>
        /// Mark ticket
        /// </summary>
        /// <param name="ticket">ticket value</param>
        public void MarkTicketAsUsed(string ticket)
        {
            var ticketEntity = this.context.SecurityTickets.FirstOrDefault(it => it.Ticket == ticket);

            if (ticketEntity == null)
            {
                return;
            }

            ticketEntity.IsUsed = true;
            this.context.Commit();
        }

        /// <summary>
        /// Mark user ticket as used
        /// </summary>
        /// <param name="ticket">ticket value</param>
        public void MarkUserTicketAsUsed(string ticket)
        {
            var ticketEntity = this.context.UserSecurityTickets.FirstOrDefault(it => it.Ticket == ticket);

            if (ticketEntity == null)
            {
                return;
            }

            ticketEntity.IsUsed = true;
            this.context.Commit();
        }

        /// <summary>
        /// Mark ticket
        /// </summary>
        /// <param name="ticket">ticket value</param>
        public void MarkCompanyTicketAsUsed(string ticket)
        {
            var ticketEntity = this.context.CompanySecurityTickets.FirstOrDefault(it => it.Ticket == ticket);

            if (ticketEntity == null)
            {
                return;
            }

            ticketEntity.IsUsed = true;
            this.context.Commit();
        }

        /// <summary>
        /// Clear invalid tickets
        /// </summary>
        public void ClearAllInvalidTickets()
        {
            var invalidSecurityTickets =
                this.context.SecurityTickets.Where(it => it.ExpiredOn >= DateTimeOffset.UtcNow || it.IsUsed);

            foreach (var ticket in invalidSecurityTickets)
            {
                this.context.SecurityTickets.Remove(ticket);
            }

            var invalidUserSecurityTickets =
                this.context.UserSecurityTickets.Where(it => it.ExpiredOn >= DateTimeOffset.UtcNow || it.IsUsed);

            foreach (var ticket in invalidUserSecurityTickets)
            {
                this.context.SecurityTickets.Remove(ticket);
            }

            var invalidCompanySecurityTickets =
                this.context.CompanySecurityTickets.Where(it => it.ExpiredOn >= DateTimeOffset.UtcNow || it.IsUsed);

            foreach (var ticket in invalidCompanySecurityTickets)
            {
                this.context.SecurityTickets.Remove(ticket);
            }

            this.context.Commit();
        }
    }
}

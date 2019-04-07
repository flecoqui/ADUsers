using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;

namespace ADUsers
{
    class Program
    {
        private static string sDomain = @"testwvd.pw";
        private static string sDefaultOU = @"OU=Users,OU=testwvd,DC=testwvd,DC=pw";
        private static string sServiceUser = @"TESTWVD\VMAdmin";
        private static string sServicePassword = @"VMP@ssw0rd123";
        public static PrincipalContext GetPrincipalContext()
        {
            PrincipalContext oPrincipalContext = new PrincipalContext
               (ContextType.Domain, sDomain, sDefaultOU,
               //ContextOptions.SimpleBind,
                                sServiceUser, sServicePassword);
            Console.WriteLine("GetPrincipalContext success");
            return oPrincipalContext;
        }
        public static UserPrincipal GetUser(string sUserName)
        {
            UserPrincipal oUserPrincipal = null;
            try
            {
                PrincipalContext oPrincipalContext = GetPrincipalContext();
                Console.WriteLine(" fining");
                oUserPrincipal =
                   UserPrincipal.FindByIdentity(oPrincipalContext, sUserName);
                Console.WriteLine(" fining done");
            }
            catch(Exception ex)
            {
                Console.WriteLine("Exception while looking for user " + sUserName + ": " + ex.Message);

            }
            return oUserPrincipal;
        }

        public static bool IsUserExisiting(string sUserName)
        {
            if (GetUser(sUserName) == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        public static UserPrincipal CreateNewUser(string sUserName, string sPassword, string sGivenName, string sSurname, string sEmailAddress)
        {
            if (!IsUserExisiting(sUserName))
            {
                PrincipalContext oPrincipalContext = GetPrincipalContext();
                Console.WriteLine(" creating context");
                UserPrincipal oUserPrincipal = new UserPrincipal(oPrincipalContext);
                if (oUserPrincipal != null)
                {
                    Console.WriteLine(" creating user");

                    oUserPrincipal.UserPrincipalName = sUserName;
                    oUserPrincipal.SetPassword(sPassword);
                    oUserPrincipal.GivenName = sGivenName;
                    oUserPrincipal.Surname = sSurname;
                    oUserPrincipal.EmailAddress = sEmailAddress;
                    oUserPrincipal.DisplayName = sGivenName + " " + sSurname;
                    oUserPrincipal.Name = sGivenName + " " + sSurname;
                    oUserPrincipal.ExpirePasswordNow();
                    //                    oUserPrincipal..PasswordNeverExpires = true;
                    Console.WriteLine(" saving user");
                    oUserPrincipal.Save();
                }
                return oUserPrincipal;
            }
            else
            {
                return GetUser(sUserName);
            }
        }
        public static bool DeleteUser(string sUserName)
        {
            try
            {
                UserPrincipal oUserPrincipal = GetUser(sUserName);
                if (oUserPrincipal != null)
                {
                    oUserPrincipal.Delete();
                    return true;
                }
            }
            catch
            {
            }
            return false;
        }
        static DirectoryEntry createDirectoryEntry(string domain, string ldapPath)
        {
            // create and return new LDAP connection with desired settings  

            DirectoryEntry ldapConnection = new DirectoryEntry(domain);
            ldapConnection.Path = "LDAP://" + ldapPath;
            ldapConnection.AuthenticationType = AuthenticationTypes.Secure;
            return ldapConnection;
        }
        static int deleteUser(DirectoryEntry myLdapConnection, String first,
                      String last )
        {
            DirectoryEntry user = myLdapConnection.Children.Find("CN=" + first + " " + last, "user");
            if (user != null)
            {
                myLdapConnection.Children.Remove(user);
                user.CommitChanges();
            }
            return 0;
        }
        static int createUser(DirectoryEntry myLdapConnection, String domain, String first,
                      String last, String description, object[] password,
                      String username,  bool enabled)
        {
            // create new user object and write into AD  

            DirectoryEntry user = myLdapConnection.Children.Add(
                                  "CN=" + username, "user");

            // User name (domain based)   
            user.Properties["userprincipalname"].Add(username + "@" + domain);

            // User name (older systems)  
            user.Properties["samaccountname"].Add(username);

            // Surname  
            user.Properties["sn"].Add(last);

            // Forename  
            user.Properties["givenname"].Add(first);

            // Display name  
            user.Properties["displayname"].Add(first + " " + last);

            // Description  
            user.Properties["description"].Add(description);

            // E-mail  
            user.Properties["mail"].Add(username + "@" + domain);


            user.CommitChanges();

            
            // enable account if requested (see http://support.microsoft.com/kb/305144 for other codes)   
            //if (enabled)
            //    user.Invoke("Put", new object[] { "userAccountControl", "512" });
            user.Properties["userAccountControl"].Value = 0x0200 | 0x10000;

            // set user's password  
            user.Invoke("SetPassword", password);

            user.CommitChanges();


            return 0;
        }
        static void ListUsers(Option option, string ldapDomain, string ldapPath)
        {
            try
            {

                option.LogInformation("Creating the LDAP Connection for domain " + ldapDomain + " and LDAP://" + ldapPath);
                DirectoryEntry myLdapConnection = createDirectoryEntry(ldapDomain, ldapPath);

                if (myLdapConnection != null)
                {
                    option.LogInformation("Listing users");
                    foreach (DirectoryEntry entry in myLdapConnection.Children)
                    {
                        if (entry.SchemaClassName == "user")
                            option.LogInformation("User Name: " + entry.Username + " Path: " + entry.Path);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception while listing the user: " + ex.Message);

            }

        }
        static void DeleteUser(Option option, string ldapDomain, string ldapPath, string userName)
        {
            try
            {
                option.LogInformation("Creating the LDAP Connection for domain " + ldapDomain + " and LDAP://" + ldapPath);

                DirectoryEntry myLdapConnection = createDirectoryEntry(ldapDomain, ldapPath);

                if (myLdapConnection != null)
                {
                    DirectoryEntry user = myLdapConnection.Children.Find("CN=" + userName, "user");
                    if (user != null)
                    {
                        option.LogInformation("Removing user: " + userName);
                        myLdapConnection.Children.Remove(user);
                        user.CommitChanges();
                        option.LogInformation("User " + userName + " removed ");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception while removing the user: " + ex.Message);

            }
        }
        static void CreateUser(Option option, string ldapDomain, string ldapPath, string userName, string firstName, string lastName, string description, string Password)
        {
            try
            {
                option.LogInformation("Creating the LDAP Connection for domain " + ldapDomain + " and LDAP://" + ldapPath);
                option.LogInformation("username: " + userName + " firstName: " + firstName + " lastName: " + lastName + " description: " + description + " password: " + Password );

                DirectoryEntry myLdapConnection = createDirectoryEntry(ldapDomain, ldapPath);
                object[] password = { Password };
                if (myLdapConnection != null)
                {
                    option.LogInformation("Creating user: " + userName);
                    createUser(myLdapConnection, ldapDomain, firstName, lastName, description, password, userName, true);
                    option.LogInformation("User " + userName + " Created" );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception while creating the user: " + ex.Message);

            }

        }
        static void Main(string[] args)
        {
            Option option = Option.ParseCommandLine(args);
            string Error = option.GetErrorMessage();
            if(!string.IsNullOrEmpty(Error))
            {
                option.LogInformation("Syntax Error: " + Error);
                option.PrintHelp();
                return;
            }
            if ((option != null)&&(string.IsNullOrEmpty(Error)))
            {
                switch(option.ADUserAction)
                {
                    case Option.Action.Add:
                        CreateUser(option, option.ldapDomain, option.ldapPath, option.userName, option.firstName, option.lastName, option.description, option.password);
                        break;
                    case Option.Action.List:
                        ListUsers(option, option.ldapDomain, option.ldapPath);
                        break;
                    case Option.Action.Remove:
                        DeleteUser(option, option.ldapDomain, option.ldapPath,option.userName);
                        break;
                    case Option.Action.Help:
                        option.PrintHelp();
                        break;
                    default:
                        option.LogInformation("ADUser: Syntax Error");
                        option.PrintHelp();
                        break;

                }

            }
            else
            {
                Option o = new Option();
                o.LogInformation("ADUser: Internal Error");
                return;
            }
            return;

        }
    }
}

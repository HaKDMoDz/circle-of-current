using System;
using System.ServiceProcess;
using System.Net;
using System.Security.Cryptography;
using System.Net.Mail;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

using NetUtilities;
using LogUtilities;

namespace PokerServer
{
    public enum RegistrationResult
    {
        Successful,
        UsernameExists,
        EmailExists,
        InvalidUsername,
        InvalidEmail,
        UnknownError
    }

    public enum ChipRetrieveResult
    {
        Successful,
        NotEnoughChipsError,
        UnknownError
    }

    public static class Database
    {
        private static readonly string playerTableName = "player";

        private static MySqlConnection dbConnection;
        private static MySqlDataReader dbReader;

        public static bool EstablishDatabaseConnection()
        {
            return EstablishDatabaseConnection(
                Properties.Settings.Default.dbhost,
                Properties.Settings.Default.dbdatabase,
                Properties.Settings.Default.dbusername,
                Properties.Settings.Default.dbpassword
                );
        }

        public static bool EstablishDatabaseConnection(string server, string db, string user, string pass)
        {
            ServiceController service = new ServiceController("MySQL");

            try
            {
                if (service.Status == ServiceControllerStatus.Running)
                {
                    Logger.Log(LogType.Event, "MySQL is already running");
                }
                else
                {
                    Logger.Log(LogType.Event, "MySQL is " + Enum.GetName(typeof(ServiceControllerStatus), service.Status));
                    Logger.Log(LogType.Event, "Starting MySQL");
                    service.Start();
                }
            }
            catch (Exception ex)
            {
                Logger.Log("While starting MySQL service", ex);
            }

            if (service.Status != ServiceControllerStatus.Running)
            {
                Logger.Log(LogType.Error, "MySQL is not running");
                return false;
            }

            string connstr = String.Format(
                "Server='{0}';Database='{1}';Uid='{2}';Pwd='{3}';",
                server, db, user, pass);

            try
            {
                Logger.Log(LogType.Event, "DB Connecting: " + connstr);
                dbConnection = new MySqlConnection(connstr);
                dbConnection.Open();

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log("While establishing database connection", ex);
                return false;
            }
        }

        public static RegistrationResult RegisterNewPlayer(string username, string email)
        {
            CloseReader();

            try
            {
                if (ValidateUsername(username) == false)
                    return RegistrationResult.InvalidUsername;

                if (ValidateEmail(email) == false)
                    return RegistrationResult.InvalidEmail;

                string query = String.Format(
                    "SELECT * FROM {0} WHERE (Name='{1}')",
                    playerTableName, username);

                MySqlCommand cmd = new MySqlCommand(query, dbConnection);
                dbReader = cmd.ExecuteReader();

                if (dbReader.HasRows)
                {
                    CloseReader();
                    return RegistrationResult.UsernameExists;
                }

                CloseReader();

                query = String.Format(
                    "SELECT * FROM {0} WHERE (Email='{1}')",
                    playerTableName, email);

                cmd = new MySqlCommand(query, dbConnection);
                dbReader = cmd.ExecuteReader();

                if (dbReader.HasRows)
                {
                    CloseReader();
                    return RegistrationResult.EmailExists;
                }

                CloseReader();

                string password = RandomPassword();

                string hashedPassword = HashPassword(password);
                

                query = String.Format(
                    "INSERT INTO {0} (Name, Password, Email, Chips, Active, RegDate) VALUES ('{1}', '{2}', '{3}', '{4:0}', '0', '{5}')",
                    playerTableName, username, hashedPassword, email, Properties.Settings.Default.playerinichips,
                    DateTime.Now.ToString("yyyy:MM:dd hh:mm:ss")
                    );
                cmd = new MySqlCommand(query, dbConnection);
                dbReader = cmd.ExecuteReader();

                CloseReader();

                Email(
                    "Poker Account Registration",
                    String.Format(
                        "Hello, you've just registered to play Circle of Current Poker! " +
                        "Your username is {0} and temporary password is {1}. " +
                        "Your account will expire on {2} unless you login at least once.",
                        username, password, DateTime.Now.AddDays(Properties.Settings.Default.playerinactivelimit).ToString("MMMM d yyyy")
                        ),
                    email
                    );

                return RegistrationResult.Successful;
            }
            catch (Exception ex)
            {
                Logger.Log("During registration", ex);
                return RegistrationResult.UnknownError;
            }
        }

        public static bool ChangePassword(LobbyPlayer player, string newpassword)
        {
            CloseReader();

            Logger.Log(LogType.Event, player.Name + " wants to change password");

            try
            {
                string username = player.Name;

                string query = String.Format(
                    "SELECT * FROM {0} WHERE (Name='{1}')",
                    playerTableName, username);

                MySqlCommand cmd = new MySqlCommand(query, dbConnection);
                dbReader = cmd.ExecuteReader();

                if (dbReader.HasRows == false)
                {
                    CloseReader();
                    return false;
                }

                CloseReader();

                query = String.Format(
                    "UPDATE {0} SET Password='{1}', Active='1' WHERE Name='{2}'",
                    playerTableName, newpassword, username);

                cmd = new MySqlCommand(query, dbConnection);
                dbReader = cmd.ExecuteReader();
                CloseReader();

                Logger.Log(LogType.Event, player.Name + " successfully changed password");

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log("During password change for " + player.Name, ex);
                return false;
            }
        }

        public static LobbyPlayer Login(string username, string password, NetTunnel tunnel, Server lobby)
        {
            CloseReader();

            Logger.Log(LogType.Event, username + " wants to login");
            try
            {
                string query = String.Format(
                    "SELECT * FROM {0} WHERE (Name='{1}' and Password='{2}')",
                    playerTableName, username, password);

                MySqlCommand cmd = new MySqlCommand(query, dbConnection);
                dbReader = cmd.ExecuteReader();

                if (dbReader.HasRows == false)
                {
                    CloseReader();
                    return null;
                }

                int chips = Properties.Settings.Default.playerinichips;

                if (dbReader.Read())
                    chips = dbReader.GetInt32("Chips");

                CloseReader();

                query = String.Format(
                    "UPDATE {0} SET Active='1' WHERE Name='{1}'",
                    playerTableName, username);

                cmd = new MySqlCommand(query, dbConnection);
                dbReader = cmd.ExecuteReader();
                CloseReader();

                Logger.Log(LogType.Event, username + " has supplied correct credentials");

                return new LobbyPlayer(username, tunnel, chips, lobby);
            }
            catch (Exception ex)
            {
                Logger.Log("During login for " + username, ex);
                return null;
            }
        }

        public static bool SaveBank(string name, int newAmount)
        {
            CloseReader();

            try
            {
                string query = String.Format(
                    "SELECT * FROM {0} WHERE (Name='{1}')",
                    playerTableName, name);

                MySqlCommand cmd = new MySqlCommand(query, dbConnection);
                dbReader = cmd.ExecuteReader();

                if (dbReader.HasRows == false)
                {
                    CloseReader();
                    return false;
                }

                CloseReader();

                query = String.Format(
                    "UPDATE {0} SET Chips='{1}' WHERE Name='{2}'",
                    playerTableName, newAmount, name);

                cmd = new MySqlCommand(query, dbConnection);
                dbReader = cmd.ExecuteReader();
                CloseReader();

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log("During SaveBank", ex);
                return false;
            }
        }

        public static void CleanInactiveUsers()
        {
            CloseReader();

            Logger.Log(LogType.Event, "Clearing inactive users from database");
            try
            {
                string query = String.Format(
                    "SELECT * FROM {0} WHERE (Active='0')",
                    playerTableName);

                MySqlCommand cmd = new MySqlCommand(query, dbConnection);
                dbReader = cmd.ExecuteReader();

                while (dbReader.Read())
                {
                    string name = dbReader.GetString("Name");
                    string email = dbReader.GetString("Email");
                    DateTime time = dbReader.GetMySqlDateTime("RegDate").GetDateTime();
                    if (time.AddDays(Properties.Settings.Default.playerinactivelimit).CompareTo(DateTime.Now) < 0)
                    {
                        query = String.Format(
                            "DELETE FROM {0} WHERE (Name='{1:0}')",
                            playerTableName, name);

                        cmd = new MySqlCommand(query, dbConnection);
                        dbReader = cmd.ExecuteReader();
                        CloseReader();

                        Logger.Log(LogType.Event, name + " was deleted from database due to inactivity");

                        Email(
                        "Poker Account Expired",
                        String.Format(
                            "Hello, your account ({0}) on Circle of Current Poker has expired.",
                            name),
                        email
                        );
                    }
                }

                CloseReader();
            }
            catch (Exception ex)
            {
                Logger.Log("While cleaning inactive users", ex);
            }
        }

        public static bool ResetPassword(string username, string email)
        {
            CloseReader();

            Logger.Log(LogType.Event, username + " wants to reset password");
            try
            {
                string query = String.Format(
                    "SELECT * FROM {0} WHERE (Name='{1}' and Email='{2}')",
                    playerTableName, username, email);

                MySqlCommand cmd = new MySqlCommand(query, dbConnection);
                dbReader = cmd.ExecuteReader();

                if (dbReader.HasRows == false)
                {
                    CloseReader();
                    Logger.Log(LogType.Event, "Bad credentials for password reset");
                    return false;
                }

                CloseReader();

                string password = RandomPassword();
                string hashedPassword = HashPassword(password);

                query = String.Format(
                    "UPDATE {0} SET Password='{1}', Active='0', RegDate='{2}' WHERE Name='{3}'",
                    playerTableName, hashedPassword, DateTime.Now.ToString("yyyy:MM:dd hh:mm:ss"), username);

                cmd = new MySqlCommand(query, dbConnection);
                dbReader = cmd.ExecuteReader();
                CloseReader();

                Logger.Log(LogType.Event, username + " has reset his/her password");

                Email(
                    "Poker Account Password Reset",
                    String.Format(
                        "Hello, your account ({0}) password for Circle of Current Poker has been reset to {1} . " +
                        "Your account will expire on {2} unless you login at least once.",
                        username, password, DateTime.Now.AddDays(Properties.Settings.Default.playerinactivelimit).ToString("MMMM d yyyy")
                        ),
                    email
                    );

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log("During password reset for " + username, ex);
                return false;
            }
        }

        private static bool ValidateUsername(string username)
        {
            if (string.IsNullOrEmpty(username))
                return false;

            if (username.StartsWith(" ") || username.EndsWith(" "))
                return false;

            if (username.Contains('\r') || username.Contains('\n') || username.Contains('\t') || username.Contains('\0'))
                return false;

            if (username.Contains('"') || username.Contains('\'') || username.Contains('\\'))
                return false;

            foreach (char c in username)
                if (char.IsLetterOrDigit(c) == false && c != '_')
                    return false;

            return true;
        }

        private static bool ValidateEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            char[] illegal = new char[] { ' ', '\t', '\r', '\n', '\0', '\'', '"', '\\', };

            foreach (char c in illegal)
            {
                if (email.Contains(c))
                    return false;
            }

            if (email.Contains('@') == false)
                return false;

            string firstHalf = email.Substring(0, email.IndexOf('@'));
            string secondHalf = email.Substring(email.IndexOf('@') + 1);

            if (firstHalf.StartsWith(".") || firstHalf.EndsWith(".") || firstHalf.Contains("..") || firstHalf.Contains("@"))
                return false;

            if (secondHalf.Contains(".") == false || secondHalf.StartsWith(".") || secondHalf.EndsWith(".") || secondHalf.Contains("..") || secondHalf.Contains("@"))
                return false;

            return true;
        }

        private static void Email(string subject, string message, string email)
        {
            try
            {
                MailMessage msg = new MailMessage(
                    String.Format(
                        "{0}@{1}",
                        Properties.Settings.Default.emailuser,
                        Properties.Settings.Default.emaildomain),
                    email,
                    subject,
                    message
                    );

                SmtpClient smtp = new SmtpClient(
                                Properties.Settings.Default.emailsmtp,
                                Properties.Settings.Default.emailsmtpport);

                smtp.EnableSsl = Properties.Settings.Default.emailsmtpusessl;
                smtp.Credentials = new NetworkCredential(
                    String.Format("{0}@{1}", Properties.Settings.Default.emailuser, Properties.Settings.Default.emaildomain),
                    Properties.Settings.Default.emailpassword);

                smtp.SendCompleted += new SendCompletedEventHandler(smtp_SendCompleted);
                smtp.SendAsync(msg, msg);
            }
            catch (Exception ex)
            {
                Logger.Log("While sending email to " + email, ex);
            }
        }

        private static void smtp_SendCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                Logger.Log(String.Format("Email Error ({0} to {1})", ((MailMessage)e.UserState).Subject, ((MailMessage)e.UserState).To[0].Address), e.Error);
                return;
            }

            if (e.Cancelled)
            {
                Logger.Log(LogType.Error, String.Format("Email Cancelled ({0} to {1})", ((MailMessage)e.UserState).Subject, ((MailMessage)e.UserState).To[0].Address));
                return;
            }

            Logger.Log(LogType.Event, String.Format("Emailed {0} to {1}", ((MailMessage)e.UserState).Subject, ((MailMessage)e.UserState).To[0].Address));
        }

        private static string HashPassword(string password)
        {
            byte[] hash = new MD5CryptoServiceProvider().ComputeHash(new UTF8Encoding().GetBytes(password));
            string hashedPassword = "";
            foreach (byte b in hash)
                hashedPassword += b.ToString("X2").ToUpperInvariant();

            return hashedPassword;
        }

        private static string RandomPassword()
        {
            return new Random().Next(int.MaxValue).ToString("X8").ToUpperInvariant();
        }

        private static void CloseReader()
        {
            CloseReader(dbReader);
        }

        private static void CloseReader(MySqlDataReader reader)
        {
            try
            {
                reader.Close();
            }
            catch { }
        }
    }
}

using System.IO;

namespace JiraClient.Utilities
{
    public static class Settings
    {
        public static readonly string USER_JQL_PATH = "./../../../UserData/JQLs.txt";
        public static readonly string USER_CREDENTIAL_PATH = "./../../../UserData/Credentials.txt";

        public static string JiraBaseURL { get; private set; }
        public static string UserName { get; private set; }
        public static string ApiToken { get; private set; }


        static Settings()
        {
            bool bSuccess = loadUserCredentials();
            if (!bSuccess)
            {
                _ = Logger.Log(MessageType.Warning, "No User Credential Saved");
                return;
            }
        }

        private static bool loadUserCredentials()
        {
            try
            {
                if (!Path.Exists(USER_CREDENTIAL_PATH))
                {
                    return false;
                }

                string[] lines = File.ReadAllLines(USER_CREDENTIAL_PATH);
                JiraBaseURL = lines[0];
                UserName = lines[1];
                ApiToken = lines[2];

                return true;
            }
            catch (Exception e)
            {
                _ = Logger.Log(MessageType.Error, e.Message);
                return false;
            }
        }
    }
}

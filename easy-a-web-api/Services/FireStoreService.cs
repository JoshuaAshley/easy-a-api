using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Storage.V1;

namespace easy_a_web_api.Services
{
    public static class FireStoreService
    {
        static string fireconfig = @"
        {
          ""type"": ""service_account"",
          ""project_id"": ""easy-a-dbad0"",
          ""private_key_id"": ""752549935c08ec09445f30423a68b359ba03996a"",
          ""private_key"": ""-----BEGIN PRIVATE KEY-----\nMIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQDKf/W5r5qfd4PJ\nLXvoFURedzpvZf/fmoVnPGybsUe7MwLt768hvB6AGD2+TsRtTvf6sKfZvLJWNrJM\nfuPANMoe78OmzZ7/y9MW+dCoIYPiQqxCNgD+tBvIc7wDgVoWEEPKsmGpX0tRhSoh\nVXoYvHBfk69sfTjFZn1+ogKeWTuYo39aGLA/v6DZ8adHtzmLrYUHvCd8HyFO8gdP\nHIJ/mdPisl8VFXI3rlcf3nyfP6VkrydMSkU8HPpRmfTmhmL8Ub7vAqvHRie4KjAN\nrVhqUbFkBwE+WoFXaf3ccSxijSTSkscjruY/NM0Ji+HpXxENtAtVP05HgphWwRKa\nnpTvTj4/AgMBAAECggEADqEA4Pm5K2UIbbut9F248zQc0hhlzEMO+R7J39OGUZVF\nVGgY2FCNSYa2yy1IjncCfidN8PJrVcQczF7sWCHYKjT7Bu1a27LfXI0OkF7fdrSH\nWj8pgj0Dts75ma06E1b4dse9O22pdMmwefkBWZEfcyApr6Y6ODSHw2Kz2rmqjwO7\nvVszBNUsoy/BMYPvZh3glDC3y6p3DancfjVY7cZzwhcGlK2WIXVNRVcMyQ2J0N2+\nnyz/h4/pz4Nq5Dp662Wc7wuGn/u3EwLd1DoxxrQzb1VA9+a+bFyjRsxLY64Zp7j5\n6m4KpjP8ByDtpuGEQwL3nOuXS5YdlYLoDmS+de6kxQKBgQD1jJQPKpYv1mUrAoAP\nqPzR2WSwZDSSMWdhQEq0J1hhKX6Y4nV303cZddlhMRhrzAvulIaFqzlCsdgWhLOh\n2WF8PEoxlkbm+CB4QNI0nR5QZCyehEuL1XkRNOR9ptz4pUinI0nqncZ0ikNyofrY\n/pdgD+yIfzL6J5Mq4DL9zqtbFQKBgQDTHlSswqOJKga4RlgSEHk9RnD80VbaDRTf\niCMKYlirfvPFkJ5dxZoEO53FNU6RoRixHJLpMpDAfuWPkMOUWFvs59kE2z+z4eqw\nk57SwR31juNfSO4X3TW/4xP8TLUn8SDVjPRt82w5dEgmL1kEvnPTNVx+Zh9gkDXn\ncQ516xK5AwKBgBSWl+d5o6QZLtxfVkt52NDjkGy1yi6j0840rM7UKbXj28iH3F+S\nrH7HAdYDXs/TCQEVFP5qL9/mt36T29frOSBIkdP8jK9bCKXskXN15Q/Khm84Gnl8\nCa2mqK3catSyTxgsfksctYIaAbO/3x+IoTqduzBqseSFovJKYBwl0vpNAoGAX2ne\n1PFmnZAr106bcRaW5LWYTPqDaSruWxQY2hGWh3Np0slIeZLbx7v10vIDn1eSZEFI\nuDINL6JqN5cyfTHt4cTq1QIN6UtaGRGufwuecaNxaVf5mzlzmFSVbLpySSn2L+1z\nbtnL+K9wCCnv7m/wwuzj75BR7/9dDUuQQITMQmcCgYEA1qQSBWvRZoEaTHG9GFcE\npHjKlO7v2rTKR9LdxY2EnbyxU4qCZfmeErpDen8Kpw8Zf+JD55/sD2AvnnV2kjQ5\ndjgx0VRjRsxWfaFGheo3DJ2WDn7T8GhpzSfnU/kvZeV2y88tW9sgQTIdZM8YWj95\n1xqaeNxRDlaZpQMOulHFt38=\n-----END PRIVATE KEY-----\n"",
          ""client_email"": ""firebase-adminsdk-gok1y@easy-a-dbad0.iam.gserviceaccount.com"",
          ""client_id"": ""111254235523821467688"",
          ""auth_uri"": ""https://accounts.google.com/o/oauth2/auth"",
          ""token_uri"": ""https://oauth2.googleapis.com/token"",
          ""auth_provider_x509_cert_url"": ""https://www.googleapis.com/oauth2/v1/certs"",
          ""client_x509_cert_url"": ""https://www.googleapis.com/robot/v1/metadata/x509/firebase-adminsdk-gok1y%40easy-a-dbad0.iam.gserviceaccount.com"",
          ""universe_domain"": ""googleapis.com""
        } ";

        static string filepath = "";
        public static FirestoreDb? DB { get; private set; }
        public static StorageClient? StorageClient { get; private set; }

        public static void SetEnvironmentVariable()
        {
            filepath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetRandomFileName())) + ".json";
            File.WriteAllText(filepath, fireconfig);
            File.SetAttributes(filepath, FileAttributes.Hidden);
            GoogleCredential credential = GoogleCredential.FromFile(filepath);
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", filepath);
            DB = FirestoreDb.Create("easy-a-dbad0");
            StorageClient = StorageClient.Create(credential);
            File.Delete(filepath);
        }
    }
}

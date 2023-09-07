﻿namespace Pursuit.Context.AD
{
    using Google.Apis.Auth.OAuth2;
    using Google.Apis.Services;
    using AdminAPIs = Google.Apis.Admin.Directory.directory_v1;

    public class GoogleSASecrets
    {
        public string type { get; set; }
        public string project_id { get; set; }
        public string private_key_id { get; set; }
        public string private_key { get; set; }
        public string client_email { get; set; }
        public string client_id { get; set; }
        public string auth_uri { get; set; }
        public string token_uri { get; set; }
        public string auth_provider_x509_cert_url { get; set; }
        public string client_x509_cert_url { get; set; }

        /*
          "type": "service_account",
          "project_id": "evolveaccess",
          "private_key_id": "8d55d27f02cb384919cff99aa5235ce10a79f8c9",
          "private_key": "-----BEGIN PRIVATE KEY-----\nMIIEvAIBADANBgkqhkiG9w0BAQEFAASCBKYwggSiAgEAAoIBAQCKbGg+8c9dnP0n\nh2N4amF3njgTaOlGr+jebDFDkEvqC3wuGPwbMWkCkpOZt+jtNui+EWTITOFr/2Vu\nyd76EwTkQ1RSdFgh4InA368oUYioz1FC3ftO8yUVP3ZP7SpMgjjKx52A65dm7MW4\nGwsebM5wrpkCTmYhsQ6PBxg0Fq5IL9YVu6OxIMJw+x+kDBFKU5Wp3yAvdRf11RY1\njlTgpSdrXjJ67sk13SNA8sFx+MIB7mPBVANxLufzZ+X4Doi7nbeIhqpoLctY+oub\n9ogNLPBSp7W9vU2ba7R02ZHKoQqRx7ie4Yvr9v3Qa4agoteCMZh5T51OOV2R8zUZ\nuzZzN9j7AgMBAAECggEAN1BXneOJ/joIDV40OP+loCOo+9Sd90G/F/Z6/ykvtMBP\nKKqSP5mQgVcqRTBxEy2wdpdDwyi5oarmkQ15HUwxVbez/9j/CNaNpXWdLErchbyG\nl+ZVkLhntqRr9kdq8jTNVfbLcSNzlk0CO24PFOLc4blbakkC1e7HRw9KNDJmBXEF\nqPsHZnGg4frYBiDWNNOV3znf8F9rJXl2rbwjpFTQo1rwfz/dFouHOSeyBFEiw4HQ\n+bgN4T6t0JAoHzIFgLvCuKZki/XgEUqjcNCOt/nNUIYBI3TI9Hs847K2v/FWzZLS\nfUiabw7cHkp2Mr72SkeUSOWdBFX7LigASRMB78oo+QKBgQDA6CW/4kIa1WMiMcRP\n3RgB2zVTHL5iPB80fbPEIHPEwfWVoSFQACOkowi/wLDfIwcpoCtFhGfu00PvQLRe\nuwkSlI9FiciOKB1rhmwP5OjZm6Wh4/CktVbI+ZBFhjFzEIRM8gHleRFwXKnMloET\n64/KKhhJne1vlCJTftvsJGIvkwKBgQC3sm9E1nrCBkEmuQpASKkZAxNw4UbP4BeQ\nitNhveBoQaXsukiZgh9G8J+hM1gmyHkakdaq6zjuzOx8uN2wReeFBx1zwMbxVUKQ\n6TIB8wjSv8D9+933v5v09Zf5jB9AUe4ismaRB5NsfQrgEXs43/FsyVv5rGfzSc/m\nQJv6QVgB+QKBgD/Mx6dlwm0zg9zsTrwHKIh8om9Bg2nj7oIizNCh1wgNChcZunXG\nBgPOc/dPWHAEGrtWoNkWCHXBY6d+Y+ksvLxra9MY1b7GX6yPQbAkCirmQmp/g7hF\nzVUczO1hi3s9zDPSmnP1jaH206W5ZSlccCrxrySx2bRcbtnkjAHWqq6HAoGAZclP\ncltN5hjFHQnHLluUpzFXImMRc7n+FK939V7a66oEoKmP9M9vOUW3jgD/RW4r/Jb2\n1fpEr72JBIsC+9ugL8wDe9JD6hGOMvGkLgRWzUBHVfSrx826Qv+a2EHWRzOeukcU\nIiSKgcC/t+y31InyIo9okW4Ao4Qw2KrQQtjWRTECgYB1/8UU5QW9T7nrLUbkmc3m\nnEs5DX4kd8usc9aO7hLID2+7opxdy8E3H1PckpOkn2wvZnTx5vXoijOJ+RVjfGcq\n/pAFc3gQkqP4ECDy2MoJPCDPwDsLqnd4+2Vd/LMzkQZvZIYmxr2uVZkGJthJctl6\nuA9eB2azSEHPRCvL0+A7nw==\n-----END PRIVATE KEY-----\n",
          "client_email": "aduserservices@evolveaccess.iam.gserviceaccount.com",
          "client_id": "114495403269381897434",
          "auth_uri": "https://accounts.google.com/o/oauth2/auth",
          "token_uri": "https://oauth2.googleapis.com/token",
          "auth_provider_x509_cert_url": "https://www.googleapis.com/oauth2/v1/certs",
          "client_x509_cert_url": "https://www.googleapis.com/robot/v1/metadata/x509/aduserservices%40evolveaccess.iam.gserviceaccount.com"
        */
    }

    

  public static class DirectoryServiceFactory {
    public static AdminAPIs.DirectoryService CreateDirectoryService(ServiceAccountCredential credentials) {
      return new AdminAPIs.DirectoryService(
        new BaseClientService.Initializer() {
          HttpClientInitializer = credentials,
          ApplicationName = "evolveaccess"
        });
    }
  }

}

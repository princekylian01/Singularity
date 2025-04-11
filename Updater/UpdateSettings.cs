namespace Singularity.Updater
{
    public static class UpdateSettings
    {
        public readonly static string GitHubRepoOwner = "princekylian01";
        public readonly static string GitHubRepoName = "Singularity";

        public static string LatestReleaseApiUrl =>
            $"https://api.github.com/repos/{GitHubRepoOwner}/{GitHubRepoName}/releases/latest";

        public static string DownloadUrl =>
            $"https://github.com/{GitHubRepoOwner}/{GitHubRepoName}/releases/latest/download/update.zip";
    }
}
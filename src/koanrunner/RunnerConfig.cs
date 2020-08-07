namespace koanrunner {

    public class RunnerConfig    
    {
        public string ExerciseDirectory { get; }
        public string TestDirectory { get; }

        public RunnerConfig(string exerciseDirectory, string testDirectory)
        {
            ExerciseDirectory = exerciseDirectory;
            TestDirectory = testDirectory;
        }
    }
}
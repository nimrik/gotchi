namespace Gotchi.MiniGames
{
    public class QuizQuestion
    {
        public string Prompt;
        public string[] Choices;
        public int CorrectIndex;

        public QuizQuestion(string prompt, int correctIndex, params string[] choices)
        {
            Prompt = prompt;
            CorrectIndex = correctIndex;
            Choices = choices;
        }
    }

    // Placeholder all-ages content to prove the loop; real question sets are a content task.
    public static class QuizBank
    {
        public static readonly QuizQuestion[] Language =
        {
            new QuizQuestion("What does 'gracias' mean in Spanish?", 1, "Hello", "Thank you", "Goodbye", "Please"),
            new QuizQuestion("Which word means 'cat' in French?", 2, "Chien", "Oiseau", "Chat", "Cheval"),
            new QuizQuestion("What is 'water' in German?", 0, "Wasser", "Brot", "Milch", "Apfel"),
            new QuizQuestion("How do you say 'friend' in Italian?", 3, "Casa", "Sole", "Luna", "Amico"),
            new QuizQuestion("What does 'arigatou' mean in Japanese?", 1, "Sorry", "Thank you", "Yes", "Welcome"),
            new QuizQuestion("Which of these means 'one' in Spanish?", 2, "Dos", "Tres", "Uno", "Cuatro"),
            new QuizQuestion("What is 'book' in French?", 0, "Livre", "Table", "Porte", "Fleur"),
            new QuizQuestion("Which word is 'good morning' in Portuguese?", 3, "Boa noite", "Obrigado", "Tchau", "Bom dia"),
        };

        public static readonly QuizQuestion[] Science =
        {
            new QuizQuestion("Which planet is known as the Red Planet?", 2, "Venus", "Jupiter", "Mars", "Saturn"),
            new QuizQuestion("What gas do plants breathe in to make food?", 1, "Oxygen", "Carbon dioxide", "Helium", "Nitrogen"),
            new QuizQuestion("How many legs does an insect have?", 3, "Four", "Eight", "Ten", "Six"),
            new QuizQuestion("What is the hardest natural material?", 0, "Diamond", "Iron", "Granite", "Gold"),
            new QuizQuestion("Water freezes at what temperature in Celsius?", 1, "10°", "0°", "100°", "-50°"),
            new QuizQuestion("Which animal is a mammal?", 2, "Shark", "Frog", "Dolphin", "Lizard"),
            new QuizQuestion("What is the largest planet in our solar system?", 3, "Earth", "Mars", "Neptune", "Jupiter"),
            new QuizQuestion("What do bees collect from flowers?", 0, "Nectar", "Sand", "Water", "Leaves"),
        };
    }
}

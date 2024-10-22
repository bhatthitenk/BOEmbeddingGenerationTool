namespace BOEmbeddingService.Interfaces;

public interface IGenerateQuestionsService
{
    /// <summary>
    /// Generate questions for RAG
    /// </summary>
    Task GenerateQuestions();
}

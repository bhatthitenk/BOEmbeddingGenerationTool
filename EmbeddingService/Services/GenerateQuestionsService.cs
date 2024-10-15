using Azure.AI.OpenAI;
using BOEmbeddingService.Interfaces;
using BOEmbeddingService.Models;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json.Nodes;

namespace BOEmbeddingService.Services
{
    public class GenerateQuestionsService : IGenerateQuestionsService
	{
        static appSettings _appSettings = Configuration.BuildAppSettings();
        string openAiEndpoint = _appSettings.openAiEndpoint;
        string openAiEmbeddingModelName = _appSettings.openAiEmbeddingModelName;
        ApiKeyCredential openAiKey = new ApiKeyCredential(_appSettings.openAiKey);


		public GenerateQuestionsService()
		{
		}

		/// <summary>
		/// Generate questions for RAG
		/// </summary>
		public async Task<List<(string, string)>> GenerateQuestions(BusinessObjectDescription description, AIModelDefinition model, string serviceName)
		{
			var prompt = """
		You are an advanced ERP (enteprise resource processing) assistant. When provided with a description of
		a business object (service) in Epicor Kinetic ERP system, you use this description to generate a set of 20 questions that this service could answer.
		
		Questions you generate can have example data marked with letter abbreviations, e.g. "Do X". Do not generate questions for modifying data, all the
		questions you create should be returned as search type questions.
		
		Only use user supplied description to determine questions to answer. Do NOT use any other data. In the generated questions you must not mention technical implementation details, such as asking for dataset.
		Do not put any fictitious data into the question - instead use letter placeholders only.
		
		Format your response as json with field "questions" containing an array of strings, ne per question generated.
		""";

			var options = new AzureOpenAIClientOptions();
			options.RetryPolicy = new System.ClientModel.Primitives.ClientRetryPolicy(2);
			options.NetworkTimeout = TimeSpan.FromMinutes(10);
			OpenAIClient openAiClient = new AzureOpenAIClient(new Uri(openAiEndpoint), openAiKey, options);

			var completion = await openAiClient.GetChatClient(model.DeploymentName).CompleteChatAsync(new ChatMessage[]
			{
		ChatMessage.CreateSystemMessage(prompt),
		ChatMessage.CreateUserMessage(description.Description),
			}, new ChatCompletionOptions
			{
				Temperature = 0.0f,
				//MaxTokens = 8000,
				ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
			});

			var questions = JsonNode.Parse(completion.Value.Content.Last().Text)["questions"].AsArray().Select(x => x.AsValue().GetValue<string>());

			var embeddings = await openAiClient.GetEmbeddingClient(openAiEmbeddingModelName).GenerateEmbeddingsAsync(questions);
			return questions.Zip(embeddings.Value.Select(x => x.Index)).Select(data => (data.First, data.Second.ToString())).ToList();
		}
	}
}

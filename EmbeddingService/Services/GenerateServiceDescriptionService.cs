using Azure.AI.OpenAI;
using BOEmbeddingService.Interfaces;
using BOEmbeddingService.Models;
using OpenAI.Chat;
using OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
namespace BOEmbeddingService.Services
{
    public class GenerateServiceDescriptionService : IGenerateServiceDescription
    {
        private readonly ICommonService _commonService;

        OpenAIService openAIService = new OpenAIServiceBuilder().Build();
        public GenerateServiceDescriptionService(ICommonService commonService)
		{
            _commonService = commonService;
        }
        public async Task<BusinessObjectDescription> GenerateServiceDescriptionAsync(string serviceName, Dictionary<string, string> interfaceDefinition, IEnumerable<CodeFile> codeFiles, AIModelDefinition model)
        {

           // OpenAIClient openAiClient = new AzureOpenAIClient(new Uri(openAiEndpoint), openAiKey);
            var prompt = $$$"""
		# Instructions
		You are an intelligent coding assistant tasked with analysing source code for Epicor Kinetic business object (service) and generating description of its functionality.
		To formulate your response, only use supplied information in the instructions as well as data sent by user. Do not use any additional sources or knowledge beyond this.
		Today is {{{DateTimeOffset.UtcNow.ToString("s")}}}
		Context will include source code in C# for several related files as well as C#-like code where method implementation has been replaced by its functional summary (in block comment). 
		Bodies of all methods have been replaced by functional description of what the code within is intended to do in a block comment. 
		Treat these as if this is what method actually does and not as comments. So if a method is CreateRecord() { /* creates a new record with fields defaulted */ } assume that this method implementation creates a new record with fields defaulted, etc.
		
		You need to generate a small summary of no more than 3 sentences as well as full description containing no more than a single page of text (4-10 paragraphs) aimed at technical audience but focused on functionality rather than code implementation details.
		
		Full description should not go into details on individual methods and functions and only provide overall functionality description for technical audience. 
		It should be sufficient to describe functional specification of the service and permit recreating it anew with no access to source code.
		When generating description focus on functionality specific to this exact business object only and do not describe generic information that is likely to be common to overall framework and all other business objects.
		Do not mention that some code is generated in your response and if applicable, include values for tolerances and constraints enforced by validations. Format your response to be easy to read.
		
		{{{Constants.KineticBusinessObjectImplementationDetails}}}
		
		User will send data in the following format:
		### Interface ###
		JSON definition of the interface in code block. This contains method declarations and corresponding summaries of functionality
		### Implementation ###
		C# implementation with method bodies replaced with corresponding comment blocks
		
		Your response should be JSON file with the following fields:
		* name - service name
		* summary - quick summary
		* description  full description
		""";

            // query Open AI for answer
            var completions = await openAIService.CompleteChatAsync(new ChatMessage[]
            {
        new SystemChatMessage(prompt),
        new UserChatMessage($$$"""
			## Interface
			```json
			{{{JsonSerializer.Serialize(interfaceDefinition)}}}
			```
			
			## Implementation
			{{{codeFiles.Select(c => $$"""
				```csharp
				// {{c.Filename}}
				
				{{c.Content}}
				```	
				""")}}}
			""")
            }, new ChatCompletionOptions
            {
                Temperature = 0.0f,
                //MaxTokens = 3000,
                //NumberOfResponses = 1,
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            });

            var result = System.Text.Json.JsonSerializer.Deserialize<BusinessObjectDescription>(completions.Value.Content.Last().Text);
            result.Name = serviceName;

            return result;
        }
    }
}

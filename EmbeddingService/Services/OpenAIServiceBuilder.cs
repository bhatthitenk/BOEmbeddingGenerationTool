using Azure.AI.OpenAI;
using BOEmbeddingService.Models;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BOEmbeddingService.Services
{
        public class OpenAIServiceBuilder
        {
            private string _endpoint = @"https://hb-dev-openai.openai.azure.com";
            private string _apiKey= "92bf567ccd344dccb7c35d0bb1567dd6";
            private AIModelDefinition _model = new ("gpt-4o", 0.00275m / 1000, 0.011m / 1000);
            private int _retryCount = 2;
            private TimeSpan _networkTimeout = TimeSpan.FromMinutes(10);

            public OpenAIServiceBuilder WithEndpoint(string endpoint)
            {
                _endpoint = endpoint;
                return this;
            }

            public OpenAIServiceBuilder WithApiKey(string apiKey)
            {
                _apiKey = apiKey;
                return this;
            }

            public OpenAIServiceBuilder WithModel(AIModelDefinition model)
            {
                _model = model;
                return this;
            }

            public OpenAIServiceBuilder WithRetryCount(int retryCount)
            {
                _retryCount = retryCount;
                return this;
            }

            public OpenAIServiceBuilder WithNetworkTimeout(TimeSpan networkTimeout)
            {
                _networkTimeout = networkTimeout;
                return this;
            }

            public OpenAIService Build()
            {
                if (string.IsNullOrEmpty(_endpoint))
                    throw new InvalidOperationException("Endpoint must be provided.");
                if (string.IsNullOrEmpty(_apiKey))
                    throw new InvalidOperationException("API Key must be provided.");
                if (_model == null)
                    throw new InvalidOperationException("Model must be provided.");

                var options = new AzureOpenAIClientOptions
                {
                    RetryPolicy = new System.ClientModel.Primitives.ClientRetryPolicy(_retryCount),
                    NetworkTimeout = _networkTimeout
                };

                var openAiClient = new AzureOpenAIClient(new Uri(_endpoint), new ApiKeyCredential(_apiKey), options);
                return new OpenAIService(openAiClient, _model);
            }
        }
 
}

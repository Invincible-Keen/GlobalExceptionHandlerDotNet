﻿using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using GlobalExceptionHandler.ContentNegotiation.Mvc;
using GlobalExceptionHandler.Tests.Exceptions;
using GlobalExceptionHandler.Tests.Fixtures;
using GlobalExceptionHandler.WebApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Shouldly;
using Xunit;

namespace GlobalExceptionHandler.Tests.WebApi.ContentNegotiationTests
{
    public class ContentNegotiationJsonWithException : IClassFixture<WebApiServerFixture>
    {
        private readonly HttpResponseMessage _response;
        private const string ContentType = "application/json";

        public ContentNegotiationJsonWithException(WebApiServerFixture fixture)
        {
            // Arrange
            const string requestUri = "/api/productnotfound";
            
            var webHost = fixture.CreateWebHostWithMvc();
            webHost.Configure(app =>
            {
                app.UseGlobalExceptionHandler(x =>
                {
                    x.Map<RecordNotFoundException>().ToStatusCode(StatusCodes.Status404NotFound)
                        .WithBody(e => new TestResponse
                        {
                            Message = "An exception occured"
                        });
                });

                app.Map(requestUri, config =>
                {
                    config.Run(context => throw new RecordNotFoundException("Record could not be found"));
                });
            });

            // Act
            var server = new TestServer(webHost);
            using (var client = server.CreateClient())
            {
                var requestMessage = new HttpRequestMessage(new HttpMethod("GET"), requestUri);
                requestMessage.Headers.Accept.Clear();
                requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(ContentType));

                _response = client.SendAsync(requestMessage).Result;
            }
        }
        
        [Fact]
        public void Returns_correct_response_type()
        {
            _response.Content.Headers.ContentType.MediaType.ShouldBe(ContentType);
        }

        [Fact]
        public void Returns_correct_status_code()
        {
            _response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Returns_correct_body()
        {
            var content = await _response.Content.ReadAsStringAsync();
            content.ShouldContain("{\"message\":\"An exception occured\"}");
        }
    }
}
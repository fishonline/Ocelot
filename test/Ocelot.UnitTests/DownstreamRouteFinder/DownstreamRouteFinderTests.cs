﻿using System.Collections.Generic;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Provider;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.DownstreamRouteFinder
{
    public class DownstreamRouteFinderTests
    {
        private readonly IDownstreamRouteFinder _downstreamRouteFinder;
        private readonly Mock<IOcelotConfigurationProvider> _mockConfig;
        private readonly Mock<IUrlPathToUrlTemplateMatcher> _mockMatcher;
        private readonly Mock<IUrlPathPlaceholderNameAndValueFinder> _finder;
        private string _upstreamUrlPath;
        private Response<DownstreamRoute> _result;
        private List<ReRoute> _reRoutesConfig;
        private Response<UrlMatch> _match;
        private string _upstreamHttpMethod;

        public DownstreamRouteFinderTests()
        {
            _mockConfig = new Mock<IOcelotConfigurationProvider>();
            _mockMatcher = new Mock<IUrlPathToUrlTemplateMatcher>();
            _finder = new Mock<IUrlPathPlaceholderNameAndValueFinder>();
            _downstreamRouteFinder = new Ocelot.DownstreamRouteFinder.Finder.DownstreamRouteFinder(_mockConfig.Object, _mockMatcher.Object, _finder.Object);
        }

        [Fact]
        public void should_return_route()
        {
            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("someUpstreamPath"))
                .And(
                    x =>
                        x.GivenTheTemplateVariableAndNameFinderReturns(
                            new OkResponse<List<UrlPathPlaceholderNameAndValue>>(new List<UrlPathPlaceholderNameAndValue>())))
                .And(x => x.GivenTheConfigurationIs(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamPathTemplate("someDownstreamPath")
                        .WithUpstreamTemplate("someUpstreamPath")
                        .WithUpstreamHttpMethod("Get")
                        .WithUpstreamTemplatePattern("someUpstreamPath")
                        .Build()
                }
                    ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Get"))
                .When(x => x.WhenICallTheFinder())
                .Then(
                    x => x.ThenTheFollowingIsReturned(new DownstreamRoute(new List<UrlPathPlaceholderNameAndValue>(),
                        new ReRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPath")
                            .Build()
                        )))
                .And(x => x.ThenTheUrlMatcherIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_return_correct_route_for_http_verb()
        {
            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("someUpstreamPath"))
                .And(
                    x =>
                        x.GivenTheTemplateVariableAndNameFinderReturns(
                            new OkResponse<List<UrlPathPlaceholderNameAndValue>>(new List<UrlPathPlaceholderNameAndValue>())))
                .And(x => x.GivenTheConfigurationIs(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamPathTemplate("someDownstreamPath")
                        .WithUpstreamTemplate("someUpstreamPath")
                        .WithUpstreamHttpMethod("Get")
                        .WithUpstreamTemplatePattern("")
                        .Build(),
                    new ReRouteBuilder()
                        .WithDownstreamPathTemplate("someDownstreamPathForAPost")
                        .WithUpstreamTemplate("someUpstreamPath")
                        .WithUpstreamHttpMethod("Post")
                        .WithUpstreamTemplatePattern("")
                        .Build()
                }
                    ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Post"))
                .When(x => x.WhenICallTheFinder())
                .Then(
                    x => x.ThenTheFollowingIsReturned(new DownstreamRoute(new List<UrlPathPlaceholderNameAndValue>(),
                        new ReRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPathForAPost")
                            .Build()
                        )))
                .BDDfy();
        }

        [Fact]
        public void should_not_return_route()
        {
            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("somePath"))
                 .And(x => x.GivenTheConfigurationIs(new List<ReRoute>
                     {
                        new ReRouteBuilder()
                        .WithDownstreamPathTemplate("somPath")
                        .WithUpstreamTemplate("somePath")
                        .WithUpstreamHttpMethod("Get")
                        .WithUpstreamTemplatePattern("somePath")
                        .Build(),   
                     }
                 ))
                 .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(false))))
                 .And(x => x.GivenTheUpstreamHttpMethodIs("Get"))
                 .When(x => x.WhenICallTheFinder())
                 .Then(
                     x => x.ThenAnErrorResponseIsReturned())
                 .And(x => x.ThenTheUrlMatcherIsCalledCorrectly())
                 .BDDfy();
        }

        private void GivenTheTemplateVariableAndNameFinderReturns(Response<List<UrlPathPlaceholderNameAndValue>> response)
        {
            _finder
                .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(response);
        }

        private void GivenTheUpstreamHttpMethodIs(string upstreamHttpMethod)
        {
            _upstreamHttpMethod = upstreamHttpMethod;
        }

        private void ThenAnErrorResponseIsReturned()
        {
            _result.IsError.ShouldBeTrue();
        }

        private void ThenTheUrlMatcherIsCalledCorrectly()
        {
            _mockMatcher
                .Verify(x => x.Match(_upstreamUrlPath, _reRoutesConfig[0].UpstreamTemplate), Times.Once);
        }

        private void GivenTheUrlMatcherReturns(Response<UrlMatch> match)
        {
            _match = match;
            _mockMatcher
                .Setup(x => x.Match(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(_match);
        }

        private void GivenTheConfigurationIs(List<ReRoute> reRoutesConfig)
        {
            _reRoutesConfig = reRoutesConfig;
            _mockConfig
                .Setup(x => x.Get())
                .ReturnsAsync(new OkResponse<IOcelotConfiguration>(new OcelotConfiguration(_reRoutesConfig)));
        }

        private void GivenThereIsAnUpstreamUrlPath(string upstreamUrlPath)
        {
            _upstreamUrlPath = upstreamUrlPath;
        }

        private void WhenICallTheFinder()
        {
            _result = _downstreamRouteFinder.FindDownstreamRoute(_upstreamUrlPath, _upstreamHttpMethod).Result;
        }

        private void ThenTheFollowingIsReturned(DownstreamRoute expected)
        {
            _result.Data.ReRoute.DownstreamPathTemplate.Value.ShouldBe(expected.ReRoute.DownstreamPathTemplate.Value);

            for (int i = 0; i < _result.Data.TemplatePlaceholderNameAndValues.Count; i++)
            {
                _result.Data.TemplatePlaceholderNameAndValues[i].TemplateVariableName.ShouldBe(
                    expected.TemplatePlaceholderNameAndValues[i].TemplateVariableName);

                _result.Data.TemplatePlaceholderNameAndValues[i].TemplateVariableValue.ShouldBe(
                    expected.TemplatePlaceholderNameAndValues[i].TemplateVariableValue);
            }
            
            _result.IsError.ShouldBeFalse();
        }
    }
}

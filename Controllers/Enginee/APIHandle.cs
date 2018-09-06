using System.IO;
using System.Text;
using API.Enums;
using API.Models.Temporary;
using Microsoft.AspNetCore.Mvc;

namespace API.Engine
{
    public partial class NeutronGeneralAPI <TRelation, TUser>
    {
        [HttpGet, HttpPost, HttpPatch, HttpDelete]
        [Route (template: "api/{resourceName}/{identifierName}/" +
            "{identifierValue}/{requestedAction}/{relationType?}/" +
            "{relatedResourceName?}/{relatedIdentifierName?}/" +
            "{relatedIdentifierValue?}")]
        public IActionResult Handle (
                string resourceName,
                string identifierName,
                string identifierValue,
                ModelAction requestedAction,
                TRelation relationType,
                string relatedResourceName,
                string relatedIdentifierName,
                string relatedIdentifierValue) =>
            Handle (new IRequest {
                    ResourceName = resourceName,
                        IdentifierName = identifierName,
                        IdentifierValue = identifierValue
                },
                requestedAction, relationType,
                new IRequest {
                    ResourceName = relatedResourceName,
                        IdentifierName = relatedIdentifierName,
                        IdentifierValue = relatedIdentifierValue
                },
                new StreamReader (Request.Body, Encoding.UTF8).ReadToEnd ());

        [HttpGet, HttpPost]
        [Route (template: "api/{resourceName}/{requestedAction}")]
        public IActionResult Handle (
                string resourceName,
                ModelAction requestedAction) =>
            Handle (new IRequest {
                    ResourceName = resourceName,
                        IdentifierName = null,
                        IdentifierValue = null
                },
                requestedAction,
                default(TRelation),
                null,
                new StreamReader (Request.Body, Encoding.UTF8).ReadToEnd ());

        [HttpDelete, HttpPost]
        [Route (template: "api/{resourceName}/{requestedAction}/{relationType}")]
        public IActionResult Handle (
                string resourceName,
                ModelAction requestedAction,
                TRelation relationType) =>
            Handle (new IRequest {
                    ResourceName = resourceName,
                        IdentifierName = null,
                        IdentifierValue = null
                },
                requestedAction,
                relationType,
                null,
                new StreamReader (Request.Body, Encoding.UTF8).ReadToEnd ());
    }
}
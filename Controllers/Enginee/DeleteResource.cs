using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using API.Engine;
using API.Engine.Extention;
using API.Enums;
using API.Models;
using API.Models.Temporary;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace API.Engine {
    public partial class NeutronGeneralAPI <TRelation, TUser>{
        private dynamic DeleteResource (
            IRequest request,
            TRelation relationType,
            IRequest relatedRequest,
            PermissionHandler permissionHandler) {
            return StatusCode (StatusCodes.Status501NotImplemented);
        }
    }
}
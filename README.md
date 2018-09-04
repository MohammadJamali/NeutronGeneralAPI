# Neutron General API (NG-API)

<!-- ![](header.png) -->

A general purpose server side application which will simply do all CRUD + R(elation)S(earch) thing for models, and will make server-side application development process super fast and convenient.

Goal: Create models, config with attributes, done :)

* **'Delete' is not implemented yet**
* **'Search' is not implemented yet**

## Get started

It is realy simple to work with NG-API. You need to learn some of attributes.

### Attributes

* \[DirectAccessAllowed]

The are two type of models in NG-API, those with `[DirectAccessAllowed]` and those without it! only models with this attribute
can be under query by clients, so if you have three classes named A, B, C and only A has this attribute, then A is the only class
which can be queried but clients can access B and C within A like this:

```
[DirectAccessAllowed]
public sealed class A : InteractiveVisualDescriptiveModel {
    public B b { get; set; }
    public C c { get; set; }
}
```

But it is not enough! If you have a direct access model, you still need to clarify type of valid queries, this can be achieved with

* \[ModelPermission (HttpRequestMethod, ModelAction, typeof (Validator))]

This attribute consist of three parts:

1. HttpRequestMethod: {Get, Post, Delete, Patch}
2. ModelAction: {Create, Read, Update, Delete, Relate}
3. Validator: Type of validator class

as an example if you write something like `[ModelPermission (HttpRequestMethod.Get, ModelAction.Read, typeof (FreeForAllValidator))]` for class A, clients only can send a HttpGet request with Read action to NG-API. when NG-API engine receive this request, it will try to determine whether this request is valid for requested resource or not, if it was a valid request, then it will try to verify the authenticity of the request, this will achieve with Validator.


What is Validator ?

Validator is a class which must implemet IAccessChainValidator<TRelation> interface and `Validate` will be called by engine whenever needed!
   If request fail to pass any validation with success, it will result query rejection with a bad request error!

* Anything except 'true' considered as error message ...

For example this validator dosen't even care about who ask this query or what is requested resource type! `FreeForAllValidator`
says allow everything to every one
```
public class FreeForAllValidator : IAccessChainValidator<Object> {
        public dynamic Validate(
            DbContext dbContext,
            string requesterID,
            IRequest request,
            string typeName,
            object typeValue,
            ModelAction modelAction,
            HttpRequestMethod requestMethod,
            Object relationType) => true;
    }
```

as conclusion, consider example below:
```
[DirectAccessAllowed]

[ModelPermission (HttpRequestMethod.Get, ModelAction.Read, typeof (FreeForAllValidator))]

[ModelPermission (HttpRequestMethod.Post, ModelAction.Create, typeof (ValidUserValidator))]
[ModelPermission (HttpRequestMethod.Post, ModelAction.Create, typeof (CreateXValidation))]

[ModelPermission (HttpRequestMethod.Patch, ModelAction.Update, typeof (ValidUserValidator))]
[ModelPermission (HttpRequestMethod.Patch, ModelAction.Update, typeof (XOwnerValidation))]
public sealed class X : InteractiveVisualDescriptiveModel {
}

public class ValidUserValidator : IAccessChainValidator<Relation> {
public dynamic Validate (
        DbContext dbContext,
        string requesterID,
        IRequest request,
        string typeName,
        object typeValue,
        ModelAction modelAction,
        HttpRequestMethod requestMethod,
        Relation relationType) =>
    (APIUtils.GetIQueryable (dbContext, "Users", false) as IQueryable<XUser>)
    .Select (user => new {
        user.Id,
            user.EmailConfirmed,
            user.ArtifactState
    })
    .Where (user =>
        user.EmailConfirmed &&
        user.ArtifactState != ArtifactState.Blocked &&
        user.Id.Equals (requesterID))
    .Any ();
}
}

public class CreateXValidation : IAccessChainValidator<Relation> { ... }
public class XOwnerValidation : IAccessChainValidator<Relation> { ... }
```

With this example we have a class named X, which will accept Read, Create, Update actions with the corresponding HttpRequests Get, Post, Patch. and it means you can read X only by get request and it is free for all, Create X only with post request only if you are valid user which will be determine by `ValidUserValidator` and you are deserve to create it which will be determine by `CreateXValidation` and Patch with same logic.

Ok, we are done with classes, lets talk about properies!

each property also can be configured using this attribute, opposite to class usage, if a request fail to pass any validation with success NG-API will set the value of that property to `null`, so if you want to protect a property and make it available only for few users to read, use `ModelPermission` for Read model action, or make it available only for few users to write, use `ModelPermission` for Write, Patch model actions.

* \[IdentifireValidatorAttribute]

Any CRUD request must be queried by model's key property, when key property is not accesseble directly (for example when user model extent from IdentityUser<TKey>) you can specify it by using this attribute, Or you can simply let users generate select queries by more than one atribute (This atribute must be uniqe for any user).

```
[JsonObject (MemberSerialization.OptIn)]

[DirectAccessAllowed]

[IdentifireValidatorAttribute (nameof (Email), typeof (EmailIdentifireValidator))]

[ModelPermission (HttpRequestMethod.Get, ModelAction.Read, typeof (FreeForAllValidator))]

[ModelPermission (HttpRequestMethod.Post, ModelAction.Update, typeof (UserOwnerValidator))]

[ModelPermission (HttpRequestMethod.Post, ModelAction.Relate, typeof (FreeForAllValidator))]
[ModelPermission (HttpRequestMethod.Delete, ModelAction.Relate, typeof (FreeForAllValidator))]
public class XUser : IdentityUser<string> {
    ...

    [Editable (false)]
    [BindNever]
    [JsonProperty]
    [ModelPermission (HttpRequestMethod.Post, ModelAction.Update, typeof (AdministratorValidator))]
    public UserType UserType { get; set; }

    public XUser () {
        CreateDateTime = DateTime.Now;
        UserType = UserType.Customer;
    }
}
```

Explanation:

Because we don't want to leak sensitive information like password hash and so on `[JsonObject (MemberSerialization.OptIn)]` was used and this will force Json Serializer to only serialize those properties which have been specified with `[JsonProperty]`.

`[DirectAccessAllowed]` : Anyone can query for this resource. <br />
`[ModelPermission (Get, Read, FreeForAllValidator)]`: Read model action, is allowed for anyone <br />
`[ModelPermission (Post, Update, UserOwnerValidator)]`: User can update it own information but UserType property! <br />
`[ModelPermission (Post, Relate, UserOwnerValidator)]`<br />
`[ModelPermission (Delete, Relate, UserOwnerValidator)]`<br />
These two lines mean that any user can Create/Delete any relation and this only allow creating relation from user resource, other side of the relation also need to permit this action too!

So what about UserType? we don't want to let a users specify thier own access level or UserType! this is a administrative task, so `[BindNever]` was used to say never get this property value from user request, `[JsonProperty]` was used to say show access level to anyone and `[ModelPermission (Post, Update, AdministratorValidator)]` used to say only administrators are allowed to update this property. we already know that `Create` query is not allowed for this resource so we don't need to create a model permision on `Create`, and no one can delete this resource after creation, and it must be created some where else! like `AccountController` ...

* \[DependentValue (HttpRequestMethod, ModelAction, typeof (Resolver), DependentOn?)]

This is the most useful attribute, with this attribute you can provide a value for a dependent property on serialization or deserialization, for example, if you have a class which must save profile pictures and there is an other property which must hold thumbnail of this original image you can use this attribute on that property to let it create thumbnail on serialization. When the time comes, API dependency resolver engine will try to resolve the resolver to determine value of the dependent property. 

Resolver must implement IDependencyResolver.

```
public class ImageModel : RootModel {
    ...

    [Required]
    [ModelPermission (HttpRequestMethod.Post, ModelAction.Create, typeof (ImageDataValidator))]
    [ModelPermission (HttpRequestMethod.Get, ModelAction.Read, typeof (ProtectImageCopyright))]
    public string Data { get; set; }

    [Required]
    [BindNever]
    [DependentValue (HttpRequestMethod.Post, ModelAction.Create, typeof (ThumbnailDependencyResolver), DependentOn: nameof(ImageModel.Data))]
    public string Thumbnail { get; set; }

    [BindNever]
    public string DataType { get; set; }

    ...
}
```

* \[RelationDependentValue (typeof (OnRelationCreatedResolver), typeof (OnReleationDeletedResolver))]

Sometimes there are some properties which must be update when new relation created, for example  every time a user wants to like a post, api will create a relation with name "like" for both objects (user and post) and invoke OnRelationCreated for properties of both types which have this attribute, for example it can increment the value of number of likes and OnReleationDeleted will invoke when user wants to unlike a post, so it can decrement the number of likes

* **Inner properties will not be covered**

Example:

```
public abstract class InteractiveVisualDescriptiveModel : VisualDescriptiveModel {
    [BindNever]
    [RelationDependentValue (typeof (LikeCountResolver), typeof (LikeCountResolver))]
    public long LikeCount { get; set; }

    [Editable (false)]
    [BindNever]
    [RelationDependentValue (typeof (BookmarkCountResolver), typeof (BookmarkCountResolver))]
    public long BookmarkCount { get; set; }

    [Editable (false)]
    [BindNever]
    [NotMapped]
    [DependentValue (HttpRequestMethod.Get, ModelAction.Read, typeof (UserBookmarkedMeResolver))]
    public bool Bookmarked { get; set; }

    [Editable (false)]
    [NotMapped]
    [BindNever]
    [DependentValue (HttpRequestMethod.Get, ModelAction.Read, typeof (UserLikedMeResolver))]
    public bool Liked { get; set; }
}

class UserBookmarkedMeResolver : UserHasRelationDependencyResolver {
    public override string GetRelationName () {
        return "Bookmark";
    }
}

class BookmarkCountResolver : IRelationDependent<Object> {
    public dynamic OnRelationEvent (
        DbContext dbContext,
        object model,
        string requesterID,
        IRequest request,
        IRequest dependentRequest,
        Object intractionType,
        HttpRequestMethod httpRequestMethod) {
        var intractionName = intractionType.ToString ();

        if (intractionName != "Bookmark") return null;

        if (httpRequestMethod != HttpRequestMethod.Post &&
            httpRequestMethod != HttpRequestMethod.Delete)
            return null;

        if (model == null) model = APIUtils.GetResource (dbContext, request) as object;
        if (model == null) return null;

        var bookmarkCountProp = model.GetType ().GetProperty (nameof (InteractiveVisualDescriptiveModel.BookmarkCount));
        var bookmarkCountValue = (long) bookmarkCountProp.GetValue (model);

        if (httpRequestMethod == HttpRequestMethod.Post) {
            bookmarkCountProp.SetValue (model, bookmarkCountValue + 1);
        } else if (httpRequestMethod == HttpRequestMethod.Delete) {
            bookmarkCountProp.SetValue (model, Math.Max (0, bookmarkCountValue - 1));
        }

        return model;
    }
}

...
```

on `IRelationDependent`, return `null` to skip or return model object to update it.

* \[IncludeAttribute]

Because of ef core lazy loading, we need to include some properties like ICollections and so on, when you declare any property with this attribute, API quary engine will include it when try to build select dynamic expression

* **ONLY direct properties will include in this version, so you can not include inner properties. This will fix on next versions.**

* Use `[BindNever]` and `[JsonIgnore]` on any readonly property.
* Use `[Required]` on properties which must have value.

## Initialize

Ok, models are done, now what?

in this part you need to initialize NG-API

1. Create an enum to define all type of relations, [1]
2. Create a class and define all relations as a child of `ModelIntraction<TRelation>` and implement two constructors for each
    * public X () { }
    * public X (ModelIntraction<TRelation> relation) : base (relation) { }
3. Create a class and implement `IApiEngineService<TRelation, TUser>` which TRelation is Relation enum type and TUser is your IdentityUser type
4. Call `builder.ConfigureAPIDatabase (new ApiEngineSetting ());` on `OnModelCreating (ModelBuilder builder)` in `IdentityDbContext<TUser>`
5. Create a controller as a child of `NeutronGeneralAPI<TRelation, TUser>` [3]

* **Relation enum must contain `Invalid` and `Global` relations!**

[1]
```
[JsonConverter(typeof(StringEnumConverter))]
public enum Relation {
    [EnumMember (Value = "Invalid")]
    Invalid = 0,

    [EnumMember (Value = "Global")]
    Global = 1,

    [EnumMember (Value = "Like")]
    Like = 2,

    [EnumMember (Value = "Bookmark")]
    Bookmark = 3,

    ...
}
```

[2]
```
namespace ... {
    public sealed class Like : ModelIntraction<Relation> {
        public Like (ModelIntraction<Relation> relation) : base (relation) { }
        public Like () { }
    }
    public sealed class Bookmark : ModelIntraction<Relation> {
        public Bookmark (ModelIntraction<Relation> relation) : base (relation) { }
        public Bookmark () { }
    }
    public sealed class Invalid : ModelIntraction<Relation> {
        public Invalid (ModelIntraction<Relation> relation) : base (relation) { }
        public Invalid () { }
    }
    public sealed class Global : ModelIntraction<Relation> {
        public Global (ModelIntraction<Relation> relation) : base (relation) { }
        public Global () { }
    }

    ...
}
```

[3]
```
public class GeneralControler : NeutronGeneralAPI<Relation, XUser> {
    public GeneralControler (
        ApplicationDbContext dbContext,
        UserManager<XUser> userManager,
        IApiEngineService<Relation, XUser> engineService
    ) : base (dbContext, userManager, engineService) { }
}
```

### How to query

The query pattern is as follow:

`api/{resourceName}/{identifierName}/{identifierValue}/{requestedAction}/{relationType?}/{relatedResourceName?}/{relatedIdentifierName?}/{relatedIdentifierValue?}‍‍`

Some examples:

* api/XUser/Email/mohammadjamalid@gmail.com/Read

This query will try to find and show a user using email property

* api/Model/Id/1e9ca894-e09f-4ce0-b04a-9b2d6cdc51d7/Read

This query will try to find and show a resource with name Model using Id property which is a GUID

api/Model/Id/1e9ca894-e09f-4ce0-b04a-9b2d6cdc51d7/Relate/Like

This is a one way relation that create a 'Like' relation between declared Model and the requester TUser

api/Model/Id/1e9ca894-e09f-4ce0-b04a-9b2d6cdc51d7/Update

Body Example on update
```
{
    "Id": "1e9ca894-e09f-4ce0-b04a-9b2d6cdc51d7",
    "ArtifactState": "NotVerified",
    "SubscribersCount": 0,
    "PostCount": 0,
    "ActivedCount": 0,
    "LikeCount": 0,
    "Title": "Official Title"
}
```

* **Model id's will ignore on model creation queries.**

Done!

Have fun :)

## Prerequisites

* ThumbnailDependencyResolver is depend on SixLabors.ImageSharp and SixLabors.ImageSharp.Drawing Version "1.0.0-beta0004"
* TargetFramework: netcoreapp2.1
* LangVersion: 7.2


## TODO List
* Implement 'Explore'
* Implement simple and complex text search
* Implement backup and data retirement strategy
* Adaptive query engine with multi key objects
* Implement delete or deactive object

<!-- ## Deployment

Add additional notes about how to deploy this on a live system -->

<!-- ## Built With

* [Dropwizard](http://www.dropwizard.io/1.0.2/docs/) - The web framework used
* [Maven](https://maven.apache.org/) - Dependency Management
* [ROME](https://rometools.github.io/rome/) - Used to generate RSS Feeds -->

<!-- ## Contributing

Please read [CONTRIBUTING.md](https://gist.github.com/PurpleBooth/b24679402957c63ec426) for details on our code of conduct, and the process for submitting pull requests to us. -->

## Authors

* **Mohammad Jamali** - *Initial work* - [MohammadJamali](https://github.com/MohammadJamali)

<!-- See also the list of [contributors](https://github.com/your/project/contributors) who participated in this project. -->

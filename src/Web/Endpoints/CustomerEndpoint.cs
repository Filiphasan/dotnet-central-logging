using Bogus;
using Carter;

namespace Web.Endpoints;

public class CustomerEndpoint : ICarterModule
{
    private static List<Customer> Customers { get; } = [];

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var customerFaker = new Faker<Customer>()
            .RuleFor(c => c.FirstName, f => f.Name.FirstName())
            .RuleFor(c => c.LastName, f => f.Name.LastName())
            .RuleFor(c => c.Email, (f, c) => f.Internet.Email(c.FirstName, c.LastName))
            .RuleFor(c => c.Country, f => f.Address.Country());

        for (var i = 0; i < 10000; i++)
        {
            var customer = customerFaker.Generate();
            customer.Id = i + 1;
            Customers.Add(customer);
        }

        var group = app.MapGroup("/api/v1/customers")
            .WithTags("Customer Endpoints");

        group.MapGet("{id:int}", GetByIdAsync);
    }

    private static Task<IResult> GetByIdAsync(int id)
    {
        return Task.FromResult(Results.Ok(Customers.FirstOrDefault(c => c.Id == id)));
    }
}

public class Customer
{
    public int Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Country { get; set; } = null!;
}
```csharp
// External member-level snippet injected from docs_extra/examples/OrderProcessingService/ProcessOrderAsync.cs
using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var finalStatus = await orderService.ProcessOrderAsync(
    new OrderRequest
    {
        OrderIdentifier = "ORD-2026-999",
        CustomerEmail = "enterprise@example.com",
        TotalAmount = new MonetaryValue(25_000m, "EUR")
    },
    cancellationSource.Token);
```

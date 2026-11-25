```csharp
// External snippet injected from docs_extra/examples/OrderProcessingService.cs
var orderService = new OrderProcessingService("AdyenEnterprise")
{
    RequireManualApprovalForHighValue = true
};

orderService.OrderStatusChanged += (_, eventArgs) =>
    Console.WriteLine($"Order {eventArgs.OrderIdentifier} transitioned to {eventArgs.UpdatedStatus}");
```

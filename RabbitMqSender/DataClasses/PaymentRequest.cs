namespace RabbitMqSender.DataClasses;
public record PaymentRequest
{
    public required RequestInfo Request { get; set; }
    public required TransactionPart DebitPart { get; set; }
    public required TransactionPart CreditPart { get; set; }
    public required string Details { get; set; }
    public required string BankingDate { get; set; }
    public AttributeWrapper? Attributes { get; set; }
}

public record RequestInfo
{
    public long Id { get; set; }
    public required Document Document { get; set; }
}

public record Document
{
    public long Id { get; set; }
    public required string Type { get; set; }
}

public record TransactionPart
{
    public required string AgreementNumber { get; set; }
    public required string AccountNumber { get; set; }
    public decimal Amount { get; set; }
    public required string Currency { get; set; }
    public required Dictionary<string, object> Attributes { get; set; }
}

public record AttributeWrapper
{
    public required List<CustomAttribute> Attribute { get; set; }
}

public record CustomAttribute
{
    public required string Code { get; set; }
    public required string Attribute { get; set; }
}
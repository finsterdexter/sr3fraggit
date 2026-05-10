namespace SR3Generator.Data.Gear.Attachments
{
    public sealed record AttachmentValidationFailure(
        IAttachmentHost Host,
        CapacityKind Kind,
        decimal Total,
        decimal Used,
        string Message);
}

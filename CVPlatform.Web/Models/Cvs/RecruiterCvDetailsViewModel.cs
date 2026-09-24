using CVPlatform.Application.Cvs;

namespace CVPlatform.Web.Models.Cvs;

public record RecruiterCvDetailsViewModel(RecruiterCvHeaderDto Header, GeneratedCvDto Cv);
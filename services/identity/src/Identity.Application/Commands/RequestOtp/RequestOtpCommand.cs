using MediatR;

namespace Identity.Application.Commands.RequestOtp;

/// <summary>
/// Case §3.1 / Osman.md sozlesmesi: "POST /auth/otp/request { gsmNumber }" - LoginPage'in
/// 2 adimli Musteri akisinin (numara gir -> "Kod Gonder" -> kod alani acilir -> verify) ilk
/// adimi. OTP simule edildigi icin (sabit "1234", gercek SMS gonderimi yok) burada tek is
/// GSM formatini dogrulamaktir; numaranin kayitli olup olmadigi yanitta SIZDIRILMAZ (ayni
/// VerifyOtpCommandHandler'daki "numara varligi sizdirilmaz" ilkesiyle tutarli) - kayitli
/// olmayan bir numara da "basarili" doner, gercek OTP dogrulamasi VerifyOtp adiminda yapilir.
/// </summary>
public sealed record RequestOtpCommand(string GsmNumber) : IRequest;

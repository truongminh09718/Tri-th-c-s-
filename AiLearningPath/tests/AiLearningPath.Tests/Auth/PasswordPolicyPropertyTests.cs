using AiLearningPath.Domain.Auth;
using FsCheck;
using FsCheck.Xunit;

namespace AiLearningPath.Tests.Auth;

/// <summary>
/// Property-based tests cho chính sách độ dài mật khẩu (<see cref="PasswordPolicy"/>).
/// Chạy tối thiểu 100 iteration với FsCheck.
/// </summary>
public class PasswordPolicyPropertyTests
{
    /// <summary>Sinh chuỗi mật khẩu có độ dài nhỏ hơn ngưỡng tối thiểu (0..MinLength-1).</summary>
    private static Gen<string> ShortPasswordGen =>
        from length in Gen.Choose(0, PasswordPolicy.MinLength - 1)
        from chars in Gen.ArrayOf(length, Arb.Default.Char().Generator)
        select new string(chars);

    /// <summary>Sinh chuỗi mật khẩu có độ dài từ ngưỡng tối thiểu trở lên.</summary>
    private static Gen<string> LongPasswordGen =>
        from length in Gen.Choose(PasswordPolicy.MinLength, PasswordPolicy.MinLength + 64)
        from chars in Gen.ArrayOf(length, Arb.Default.Char().Generator)
        select new string(chars);

    // Feature: ai-learning-path, Property 1: Mật khẩu ngắn luôn bị từ chối
    [Property(MaxTest = 100)]
    public Property ShortPasswords_AreAlwaysRejected()
    {
        return Prop.ForAll(Arb.From(ShortPasswordGen), password =>
        {
            var result = PasswordPolicy.Validate(password);
            return (!result.IsValid)
                .Label($"length={password.Length} expected invalid but was valid");
        });
    }

    // Feature: ai-learning-path, Property 1: Mật khẩu ngắn luôn bị từ chối
    // Thuộc tính bổ sung: mật khẩu có độ dài >= MinLength luôn được chấp nhận.
    [Property(MaxTest = 100)]
    public Property LongPasswords_AreAlwaysAccepted()
    {
        return Prop.ForAll(Arb.From(LongPasswordGen), password =>
        {
            var result = PasswordPolicy.Validate(password);
            return result.IsValid
                .Label($"length={password.Length} expected valid but was invalid");
        });
    }
}

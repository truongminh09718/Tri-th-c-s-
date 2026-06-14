using AiLearningPath.Domain.Auth;
using FsCheck;
using FsCheck.Xunit;

namespace AiLearningPath.Tests.Auth;

/// <summary>
/// Property-based tests cho <see cref="PasswordHasher"/>.
/// Chạy tối thiểu 100 iteration với FsCheck.
/// </summary>
public class PasswordHasherProperties
{
    // Feature: ai-learning-path, Property 2: Băm mật khẩu round-trip
    [Property(MaxTest = 100)]
    public Property Hash_Then_Verify_Roundtrips(NonNull<string> password)
    {
        string pwd = password.Get;
        string hash = PasswordHasher.Hash(pwd);

        // Round-trip: mật khẩu gốc luôn xác minh được với hash của chính nó.
        bool verifies = PasswordHasher.Verify(pwd, hash);

        // Hash phải khác mật khẩu thuần (lưu dưới dạng băm, không lưu plaintext - R1.4).
        bool hashDiffersFromPlaintext = !string.Equals(hash, pwd, System.StringComparison.Ordinal);

        return (verifies && hashDiffersFromPlaintext)
            .Label($"verify={verifies}, hashDiffers={hashDiffersFromPlaintext} for password length {pwd.Length}");
    }

    // Feature: ai-learning-path, Property 2: Băm mật khẩu round-trip
    [Property(MaxTest = 100)]
    public Property Verify_Fails_For_Different_Password(NonNull<string> password, NonNull<string> other)
    {
        string pwd = password.Get;
        string different = other.Get;

        // Chỉ kiểm thử khi hai mật khẩu thực sự khác nhau.
        return (!string.Equals(pwd, different, System.StringComparison.Ordinal)).Implies(() =>
        {
            string hash = PasswordHasher.Hash(pwd);
            return !PasswordHasher.Verify(different, hash);
        });
    }
}

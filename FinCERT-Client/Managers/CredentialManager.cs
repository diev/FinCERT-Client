#region License
/*
Copyright 2022-2024 Dmitrii Evdokimov
Open source software

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/
#endregion

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.Win32.SafeHandles;

namespace FincertClient.Managers;
using static NativeMethods;

public enum CredentialType
{
    Generic = 1,
    DomainPassword,
    DomainCertificate,
    DomainVisiblePassword,
    GenericCertificate,
    DomainExtended,
    Maximum,
    MaximumEx = Maximum + 1000,
}

/// <summary>
/// Windows Credential Manager credential
/// </summary>
/// <param name="CredentialType"></param>
/// <param name="TargetName"></param>
/// <param name="UserName"></param>
/// <param name="Password"></param>
public record Credential(CredentialType CredentialType, string TargetName, string? UserName, string? Password)
{
    public override string ToString()
        => $"CredentialType: {CredentialType}, TargetName: {TargetName}, UserName: {UserName}, Password: {Password}";
}

internal static class CredentialManager
{
    public static Credential ReadCredential(string targetName)
    {
        if (targetName.Contains('*'))
        {
            if (CredEnumerate(targetName, 0, out int count, out nint pCredentials))
            {
                if (count > 1)
                    throw new Exception($"Windows Credential Manager has more '{targetName}' entries ({count}).");

                nint credential = Marshal.ReadIntPtr(pCredentials, 0);
                var cred = Marshal.PtrToStructure(credential, typeof(NativeCredential));
                return ReadFromNativeCredential((NativeCredential)cred!);
            }

            throw new Exception($"Windows Credential Manager has no '{targetName}' entries.");
        }

        if (CredRead(targetName, CredentialType.Generic, 0, out nint nCredPtr))
        {
            using CriticalCredentialHandle critCred = new(nCredPtr);
            var cred = critCred.GetNativeCredential();
            return ReadFromNativeCredential(cred);
        }

        throw new Exception($"Windows Credential Manager has no '{targetName}' entries.");
    }

    public static void WriteCredential(string targetName, string userName, string secret)
    {
        NativeCredential credential = new()
        {
            AttributeCount = 0,
#if NET7_0_OR_GREATER
            Attributes = nint.Zero,
            Comment = nint.Zero,
            TargetAlias = nint.Zero,
#else
            Attributes = IntPtr.Zero,
            Comment = IntPtr.Zero,
            TargetAlias = IntPtr.Zero,
#endif
            Type = CredentialType.Generic,
            Persist = (uint)CredentialPersistence.LocalMachine,
            CredentialBlobSize = 0,
            TargetName = Marshal.StringToCoTaskMemUni(targetName),
            CredentialBlob = Marshal.StringToCoTaskMemUni(secret),
            UserName = Marshal.StringToCoTaskMemUni(userName ?? Environment.UserName)
        };

        if (secret != null)
        {
            byte[] byteArray = Encoding.Unicode.GetBytes(secret);

            if (byteArray.Length > 512 * 5)
                throw new ArgumentOutOfRangeException(nameof(secret), "The secret message has exceeded 2560 bytes.");

            credential.CredentialBlobSize = (uint)byteArray.Length;
        }

        bool written = CredWrite(ref credential, 0);

        Marshal.FreeCoTaskMem(credential.TargetName);
        Marshal.FreeCoTaskMem(credential.CredentialBlob);
        Marshal.FreeCoTaskMem(credential.UserName);

        if (!written)
        {
            int lastError = Marshal.GetLastWin32Error();
            throw new Exception(string.Format("CredWrite failed with the error code {0}.", lastError));
        }
    }

    public static Credential[] EnumerateCrendentials(string? filter = null)
    {
        if (CredEnumerate(filter, 0, out int count, out nint pCredentials))
        {
            Credential[] result = new Credential[count];

            for (int n = 0; n < count; n++)
            {
                nint credential = Marshal.ReadIntPtr(pCredentials, n * Marshal.SizeOf(typeof(nint)));
                var cred = Marshal.PtrToStructure(credential, typeof(NativeCredential));
                result[n] = ReadFromNativeCredential((NativeCredential)cred!);
            }

            return result;
        }

        int lastError = Marshal.GetLastWin32Error();
        throw new Win32Exception(lastError);
    }

    private enum CredentialPersistence : uint
    {
        Session = 1,
        LocalMachine,
        Enterprise
    }

    private static Credential ReadFromNativeCredential(NativeCredential credential)
    {
        string targetName = Marshal.PtrToStringUni(credential.TargetName)!;
        string? userName = Marshal.PtrToStringUni(credential.UserName);
        string? secret = null;

#if NET7_0_OR_GREATER
        if (credential.CredentialBlob != nint.Zero)
#else
        if (credential.CredentialBlob != IntPtr.Zero)
#endif
        {
            secret = Marshal.PtrToStringUni(credential.CredentialBlob, (int)credential.CredentialBlobSize / 2);
        }

        return new Credential(credential.Type, targetName, userName, secret);
    }

    sealed class CriticalCredentialHandle : CriticalHandleZeroOrMinusOneIsInvalid
    {
        public CriticalCredentialHandle(nint preexistingHandle)
        {
            SetHandle(preexistingHandle);
        }

        public NativeCredential GetNativeCredential()
        {
            if (!IsInvalid)
            {
                var cred = Marshal.PtrToStructure(handle, typeof(NativeCredential));
                return (NativeCredential)cred!;
            }

            throw new InvalidOperationException("Invalid CriticalHandle!");
        }

        protected override bool ReleaseHandle()
        {
            if (!IsInvalid)
            {
                CredFree(handle);
                SetHandleAsInvalid();

                return true;
            }

            return false;
        }
    }
}

internal static partial class NativeMethods
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct NativeCredential
    {
        public uint Flags;
        public CredentialType Type;
        public nint TargetName;
        public nint Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public nint CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public nint Attributes;
        public nint TargetAlias;
        public nint UserName;
    }

#if NET7_0_OR_GREATER
    [LibraryImport("Advapi32.dll", EntryPoint = "CredReadW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CredRead(string target, CredentialType type, int reservedFlag, out nint credentialPtr);

    [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool CredWrite(ref NativeCredential userCredential, uint flags); //TODO

    [LibraryImport("Advapi32", EntryPoint = "CredEnumerateW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CredEnumerate(string? filter, int flag, out int count, out nint pCredentials);

    [LibraryImport("Advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CredFree(nint cred);
#else
    [DllImport("Advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CredRead(string target, CredentialType type, int reservedFlag, out nint credentialPtr);

    [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool CredWrite(ref NativeCredential userCredential, uint flags); //TODO

    [DllImport("Advapi32.dll", EntryPoint = "CredEnumerateW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CredEnumerate(string? filter, int flag, out int count, out nint pCredentials);

    [DllImport("Advapi32.dll", EntryPoint = "CredFree", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CredFree(nint cred);
#endif
}

public class CredManagerException : Exception
{
    const string message = "'{0}' не указан в Диспетчере учетных данных Windows.";

    public CredManagerException()
        : base() { }

    public CredManagerException(string paramName)
        : base(string.Format(message, paramName)) { }

    public CredManagerException(string paramName, Exception inner)
        : base(string.Format(message, paramName), inner) { }
}

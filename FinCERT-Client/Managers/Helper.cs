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

namespace FincertClient.Managers;

internal static class Helper
{
    public static string PathCombine(string dir, string name)
    {
        string file = string.Concat(name.Split(Path.GetInvalidFileNameChars())).Trim();
        return Path.Combine(dir, file.Length > 0 ? file : "--");
    }
}

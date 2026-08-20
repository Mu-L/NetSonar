using System.Text.RegularExpressions;
using Fallout.Common.Tooling;

namespace build;

public static class LinuxAppBundle
{
    private const string FlatpakManifest = """
                                          # your app's id which ideally should match the manifest file name
                                          app-id: {0}
                                          runtime: org.freedesktop.Platform
                                          runtime-version: "23.08" # version of the runtime
                                          sdk: org.freedesktop.Sdk # the sdk to use
                                          command: {1} # the command to run, basically the command that starts the app
                                          finish-args:
                                            # Flatpaks run in sandbox mode. This is the list of permissions we need
                                            # As we're running an Avalonia App, we need to specify that we want access
                                            # to the window infrastructure of the host
                                            # X11 + XShm access
                                            - --socket=x11
                                            - --share=ipc
                                            # Wayland access
                                            #- --socket=wayland
                                            # GPU acceleration if needed
                                            - --device=dri
                                            # Needs to talk to the network:
                                            - --share=network
                                            # for more information visit https://docs.flatpak.org/en/latest/sandbox-permissions.html#sandbox-permissions

                                          modules:
                                            # This is our app and the ;build' instructions for it
                                            - name: {1}
                                              buildsystem: simple
                                              # In our case we've already run `dotnet publish` so we just need to copy the
                                              # outputs to the right place
                                              build-commands:
                                                - mkdir -p /app/bin
                                                - mv ./app-sources /app/bin/app-sources
                                                # allow our app to be executed
                                                - chmod +x /app/bin/app-sources/Flaco
                                                # create a symlink to the executable
                                                - ln -s /app/bin/app-sources/Flaco /app/bin/Flaco
                                              # This is the list of files/directories we want to copy to the flatpak
                                              # for more elaborated builds we could fetch a zip file, git repository or
                                              # even build from source however that comes with different challenges
                                              # so we'll leave that for another time
                                              sources:
                                                - type: dir
                                                  # This is the path on the host machine './flatpak/{1}'
                                                  # but since we're running the flatpak-builder command in the flatpak directory
                                                  # we'll omit the other one
                                                  path: {1}
                                                  # This is the path inside the flatpak which gets copied to the root of the flatpak
                                                  dest: app-sources
                                          """;
    public static string GetFlatpakManifest(string appId, string softwareName)
    {
        return string.Format(FlatpakManifest, appId, softwareName);
    }

    public const string AppImageGitHubUrl = "https://github.com/AppImage/AppImageKit";

    public static string GetAppImageDesktopFile(Build build)
    {
        return $"""
                [Desktop Entry]
                Type=Application

                Name={build.SoftwareName}
                Comment={build.SoftwareSummary.ReplaceLineEndings(";")}
                Categories=Network;Monitor;
                Keywords={build.SoftwarePackageTags}

                Icon={build.SoftwareName}
                Exec={build.SoftwareName}
                Terminal=false
                SingleMainWindow=true
                """;
    }

    public static string GetAppImageAppRunFile(string softwareName)
    {
        return $$"""
               #!/bin/bash

               # The purpose of this custom AppRun script is to enablesymlinking the AppImage and invoking the corresponding
               # binary depending on which symlink name was used to invoke the AppImage.
               #
               # It also provides some additional help parameters in order to allow faster familiarization with functionality
               # embedded in this AppImage.

               HERE="$(dirname "$(readlink -f "${0}")")"
               export PATH="${HERE}/usr/bin/":"${PATH}"

               exec '{{softwareName}}' $@
               """;
    }

    public static string GetAppImageAppDataXmlFile(Build build)
    {
        var summary = Regex.Replace(build.SoftwareSummary, @"\r\n?|\n", " ", RegexOptions.Multiline)
            .Replace('.', ';');

        if (summary.Length > 78)
        {
            summary = summary[..78];
        }

        return $"""
           <?xml version="1.0" encoding="UTF-8"?>
           <component type="desktop-application">
             <id>{build.SoftwareRDNS}</id>

             <name>{build.SoftwareName}</name>
             <metadata_license>FSFAP</metadata_license>
             <project_license>{build.SoftwareLicense}</project_license>
             <content_rating type="oars-1.0" />
             <summary>{summary}</summary>

             <description>
               <p>{build.SoftwareDescription}</p>
             </description>

             <categories>
               <category>Network</category>
               <category>Monitor</category>
             </categories>

             <supports>
               <control>pointing</control>
               <control>keyboard</control>
               <control>touch</control>
             </supports>

             <recommends>
               <display_length compare="ge">760</display_length>
             </recommends>

             <launchable type="desktop-id">{build.SoftwareRDNS}.desktop</launchable>

             <url type="homepage">{build.SoftwareRepositoryUrl}</url>
             <developer_name>{build.SoftwareAuthors}</developer_name>
             <update_contact>tiago_caza_AT_hotmail.com</update_contact>

             <provides>
               <binary>{build.SoftwareName}</binary>
             </provides>

           </component>
           """;
    }

    public static bool IsFuseAvailable()
    {
        using var result = ProcessTasks.StartShell("ldconfig -p | grep libfuse.so.2", timeout:3000);
        result.WaitForExit();
        return result.ExitCode == 0;
    }
}
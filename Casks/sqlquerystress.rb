cask "sqlquerystress" do
  version "0.1.0-beta"

  on_arm do
    sha256 "1bf19d8b074afc34ce77f60b26f99c92d9e601ef686e9922f1a92894444167af"
    url "https://github.com/TruePadawan/SqlQueryStress.Avalonia/releases/download/v#{version}/SqlQueryStress-#{version}-osx-arm64.zip"
  end
  on_intel do
    sha256 "a0cdc81b0554d0cf7e2a8c926555ca094474edfc197013e23c16e714385be30b"
    url "https://github.com/TruePadawan/SqlQueryStress.Avalonia/releases/download/v#{version}/SqlQueryStress-#{version}-osx-x64.zip"
  end

  name "SqlQueryStress"
  desc "Cross-platform SQL Server load testing utility"
  homepage "https://github.com/TruePadawan/SqlQueryStress.Avalonia"

  app "SqlQueryStress.app"

  postflight do
    system_command "/usr/bin/xattr",
                   args: ["-cr", "#{appdir}/SqlQueryStress.app"],
                   sudo: false
  end
end

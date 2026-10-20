cask "sqlquerystress" do
  version "0.1.0-beta"

  on_arm do
    sha256 "9d9c3624ff605b2a33659a111fe38e8a3418a17f549107a9f4f0898dbf591950"
    url "https://github.com/TruePadawan/SqlQueryStress.Avalonia/releases/download/v#{version}/SqlQueryStress-#{version}-osx-arm64.zip"
  end
  on_intel do
    sha256 "adbb84c562ba936c27b42d30ab9e1e96faa115d44eeb63c270ce5fd4350e3b1b"
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

// Optional native check against the receiver's actual Qt argument splitter.
#include <QCoreApplication>
#include <QFile>
#include <QJsonArray>
#include <QJsonDocument>
#include <QJsonObject>
#include <QProcess>
#include <cstdio>

int main(int argc, char **argv) {
    QCoreApplication app(argc, argv);
    if (app.arguments().size() != 2) return 2;
    QFile file(app.arguments()[1]);
    if (!file.open(QIODevice::ReadOnly)) return 3;
    const auto fixtures = QJsonDocument::fromJson(file.readAll()).array();
    if (fixtures.isEmpty()) return 4;
    for (const auto &fixture : fixtures) {
        const auto item = fixture.toObject();
        const auto fields = QProcess::splitCommand(item["Arguments"].toString());
        const auto options = QProcess::splitCommand(item["Options"].toString());
        if (fields.size() != options.size() + 2 || fields[0] != "-n" ||
            fields[1] != item["Name"].toString() || fields.mid(2) != options)
            return 5;
        if (fields[1].toUtf8().size() + 13 > 63) return 6;
    }
    std::printf("PASS: %d generated names round-trip through Qt QProcess::splitCommand and UTF-8.\n", int(fixtures.size()));
    return 0;
}

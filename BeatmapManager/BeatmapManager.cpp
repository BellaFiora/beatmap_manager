#define WIN32_LEAN_AND_MEAN

#include <windows.h>
#include <winsock2.h>
#include <ws2tcpip.h>
#include <stdlib.h>
#include <stdio.h>
#include <iostream>*
#include <sstream>
#include <cstddef>
#include <regex>
#include <string>

// Need to link with Ws2_32.lib, Mswsock.lib, and Advapi32.lib
#pragma comment (lib, "Ws2_32.lib")
#pragma comment (lib, "Mswsock.lib")
#pragma comment (lib, "AdvApi32.lib")

#define HOST "localhost"
#define PORT "8080"
#define RECVBUFLEN 1024
SOCKET sock = INVALID_SOCKET;
std::regex pattern(R"((\d{1,3})$)");

int openConnection() {
	WSADATA wsaData;
	struct addrinfo* info = NULL, * ptr = NULL, hints;
	int result = WSAStartup(MAKEWORD(2, 2), &wsaData);
	if (result != 0) {
		std::cout << "WSAStartup failed with error: " << result << std::endl;
		return 1;
	}
	ZeroMemory(&hints, sizeof(hints));
	hints.ai_family = AF_INET;
	hints.ai_socktype = SOCK_STREAM;
	hints.ai_protocol = IPPROTO_TCP;
	result = getaddrinfo(HOST, PORT, &hints, &info);
	if (result != 0) {
		std::cout << "getaddrinfo failed with error: " << result << std::endl;
		WSACleanup();
		return 1;
	}
	for (ptr = info; ptr != NULL; ptr = ptr->ai_next) {
		sock = socket(ptr->ai_family, ptr->ai_socktype, ptr->ai_protocol);
		if (sock == INVALID_SOCKET) {
			std::cout << "socket failed with error: " << WSAGetLastError() << std::endl;
			WSACleanup();
			return 1;
		}
		result = connect(sock, ptr->ai_addr, (int)ptr->ai_addrlen);
		if (result == SOCKET_ERROR) {
			closesocket(sock);
			sock = INVALID_SOCKET;
			continue;
		}
		break;
	}
	freeaddrinfo(info);
	if (sock == INVALID_SOCKET) {
		std::cout << "Unable to connect to BellaFiora!" << std::endl;
		WSACleanup();
		return 1;
	}
	std::cout << "Connected to BellaFiora!" << std::endl;
	return 0;
}

void closeConnection() {
	if (shutdown(sock, SD_SEND) == SOCKET_ERROR)
		std::cout << "shutdown failed with error: " << WSAGetLastError() << std::endl;
	closesocket(sock);
	WSACleanup();
}

int QueryResult(std::string query) {
	std::ostringstream ss;
	ss << "POST / HTTP/1.1\r\nHost: " << HOST << ":" << PORT << "\r\nContent-Type: text/plain\r\nContent-Length: " << query.length() << "\r\n\r\n" << query;
	std::string post_request = ss.str();
	std::cout << std::endl << "Query: " << query << std::endl;
	if (send(sock, post_request.c_str(), post_request.length(), 0) == SOCKET_ERROR) {
		std::cout << "ko (" << WSAGetLastError() << ")" << std::endl;
		return -1;
	}
	char recvbuf[RECVBUFLEN];
	std::memset(recvbuf, 0, sizeof(recvbuf));
	int bytesReceived = recv(sock, recvbuf, RECVBUFLEN - 1, 0);
	if (bytesReceived == 0) {
		std::cout << "Error:" << WSAGetLastError() << std::endl;
		return -1;
	}
	recvbuf[bytesReceived] = '\0';
	const std::string& response(recvbuf);
	std::smatch match;
	if (!std::regex_search(response, match, pattern)) {
		std::cout << "Error: no result code found" << std::endl;
		return -1;
	}
	int r = std::stoi(match[0].str());
	if (r < 0 || r > 255) {
		std::cout << "Error: invalid result code" << std::endl;
		return -1;
	}
	std::cout << "Result code: " << r << std::endl;
	return r;
}

int JsonifyOsuFile(std::string osuFile, std::string jsonFile) {
	return QueryResult("1," + osuFile + "," + jsonFile);
}

int killBellaFiora() {
	return QueryResult("0");
}

int main()
{
	if (openConnection() != 0) return 1;
	JsonifyOsuFile(
		"C:\\Users\\Bo_wo\\Desktop\\New folder (3)\\yomiyori.osu",
		"C:\\Users\\Bo_wo\\Desktop\\New folder (3)\\map.json");
	QueryResult("3");
	killBellaFiora();
	closeConnection();
	return 0;
}
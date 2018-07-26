/***
*
*	Copyright (c) 1996-2001, Valve LLC. All rights reserved.
*	
*	This product contains software technology licensed from Id 
*	Software, Inc. ("Id Technology").  Id Technology (c) 1996 Id Software, Inc. 
*	All Rights Reserved.
*
*   This source code contains proprietary and confidential information of
*   Valve LLC and its suppliers.  Access to this code is restricted to
*   persons who have executed a written SDK license with Valve.  Any access,
*   use or distribution of this code by or to any unlicensed person is illegal.
*
****/

using System;
using System.Net;

namespace SharpLife.Networking.Shared
{
    public static class NetUtilities
    {
        /// <summary>
        /// Converts a string to an IP address
        /// </summary>
        /// <param name="address">String containing a valid IP address, optionally followed by a port</param>
        /// <returns></returns>
        /// <exception cref="FormatException">If the IP address is not well formed</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the port is not in the valid range</exception>
        public static IPEndPoint StringToIPAddress(string address, int defaultPort)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (address == NetConstants.LocalHost)
            {
                return new IPEndPoint(IPAddress.Loopback, defaultPort);
            }

            //GoldSource uses a colon to separate IP and port
            //Unfortunately since IPv6 also does this, we have to detect whether it's IPv4 or IPv6
            //This method only works if the input is valid, but it's going to handle failure properly

            IPAddress ip;
            string portString;

            if (address.Contains('.'))
            {
                //If it's dots, assume IPv4

                var colon = address.LastIndexOf(':');

                string ipString;

                if (colon != -1)
                {
                    ipString = address.Substring(0, colon);
                    portString = address.Substring(colon + 1);
                }
                else
                {
                    ipString = address;
                    portString = defaultPort.ToString();
                }

                ip = IPAddress.Parse(ipString);
            }
            else
            {
                //Try to parse as IPv6

                //Parse will successfully parse Ipv6 addresses of the form "[<address>]:port"
                //Let this parse the IP, we'll check for the port part
                //this will also validate the string, so we know it must be a valid representation
                ip = IPAddress.Parse(address);

                var ipEnd = address.IndexOf("]:");

                if (ipEnd != -1)
                {
                    //A complete URL may continue after the port, so we intentionally grab the entire rest so the conversion below can validate
                    portString = address.Substring(ipEnd + 2);
                }
                else
                {
                    portString = defaultPort.ToString();
                }
            }

            //Original engine behavior would just use port 0
            if (!int.TryParse(portString, out var port))
            {
                throw new FormatException($"IP port \"{portString}\" is invalid");
            }

            return new IPEndPoint(ip, port);
        }
    }
}

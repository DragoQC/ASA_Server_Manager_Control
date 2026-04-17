I need in the vpn area the endpoint would normaly be the app itself. cause the controll app IS the VPN server.
The port shouldnt be filled.
i just want us aligned on that cause what i wanna add next when everything is setup correctly is a invite for the other servers.
the other servers will listen on like theisland.dragoqc.com/api/admin/invite so that we can push the wireguard config and say to them heres the endpoint you can contact me : control.dragoqc.com:51820
Thats my endgoal and if the preshared key is filled then it will be sended trough web to the remote webserver so it can configue its vpn itself.

Also fields that are required should pop an error under them if not filled like dns is required we could also prefill it with 1.1.1.1

In the invitation page. we should ourself create a db entry for the server so endpoiint for rechable things (theisland.dragoqc.com) for example and we would always know where to ping for data. then we should also store the api key in the server info cause each servers has one key so i want basicaly a server table that will store that.
so for the server model : 
UUID
apikey
url
and thats it i think
and also the base model created att and modified at

I want to also add in the control the cluster key to be able to push it to the other servers config file so that each cluster gets his cluster key from the control

i want to add the folder /opt/asa/cluster on the control and once the servers are connected to the vpn they will link their /opt/asa/cluster to the control so i guess they will each on their side mount the remote server /opt/asa/cluster to their own when vpn setup is done thats not on your side thats on each server side but the /opt/asa/cluster share on the control must be hmm done on your side?? to allow anyone the write access to it?? dont know need help for that
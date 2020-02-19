using System;
// credit: https://www.fantasynamegenerators.com/fantasy-animal-names.php

namespace EcoBuilder.Archie
{
    public static class name_generator
    {
        static string[] nm1 = new string[] {"Alba","Alli","Ana","Ante","Arma","Barra","Bea","Buffa","Cani","Cari","Cate","Coyo","Cri","Cro","Croco","Drago","Ele","Feli","Fla","Flami","Gaze","Gira","Hexa","Hone","Jagu","Komo","Leo","Locu","Mana","Moo","Pa","Pea","Peli","Phea","Porcu","Rhi","Rhino","Sala","Sco","Sku","Sna","Snai","Spa","Spi","Squi","Sti","Toa","Ursa","Vi","Wea","Wha","Woo"};
        static string[] nm2 = new string[] {"b","c","d","f","g","k","l","m","n","p","q","r","s","t","v","w","x","z","b","c","d","f","g","k","l","m","n","p","q","r","s","t","v","w","x","z","br","cr","dr","gr","kr","pr","sr","st","str","bl","cl","fl","gl","kl","pl","sl","vl","cs","ds","fs","gs","ks","ls","ms","ns","ps","rs","ss","ts","bb","cc","dd","ff","gg","kk","ll","mm","nn","pp","rr","ss","tt","ww","zz"};
        static string[] nm3 = new string[] {"a","o","i","e","u","aa","oo","ee","au","ou","ea","eo"};
        static string[] nm4 = new string[] {"","","c","d","k","l","m","n","p","r","s","t","x","cs","ks","ps","rs","ts","st"};
        static string[] nm5 = new string[] {"","","","","","a","o","i","e","u","ee","ea","eo"};
        static string[] nm6 = new string[] {"Alb","All","Alp","Ant","Arach","Arm","Bab","Badg","Barr","Beav","Bis","Buff","Cam","Cat","Chick","Cobr","Coy","Croc","Dol","Don","Drag","Eag","El","Eleph","Fal","Falc","Fer","Flam","Gaz","Ger","Gir","Guin","Hedg","Hex","Hipp","Hor","Horn","Humm","Hyen","Jag","Kang","Koal","Kom","Komod","Leop","Lob","Mag","Mall","Mant","Mon","Mong","Mos","Mosq","Mul","Oct","Ost","Pan","Pand","Parr","Pel","Pen","Peng","Pon","Por","Quad","Rab","Rabb","Rac","Racc","Rhin","Sal","Sar","Scor","Ser","Serp","Skun","Snak","Spar","Sparr","Spid","Stin","Sting","Ter","Term","Tetr","Tuc","Tur","Turt","Vul","Vult","Wal","Wall","War","Wart","Wol","Wolv","Wom","Wor","Zeb"};
        static string[] nm7 = new string[] {"abura","aby","acle","acuda","adger","adillo","alo","amander","amel","ander","anzee","api","arak","aroo","aros","atee","atross","ecta","een","ela","elope","ena","eon","ephant","erine","erpillar","eton","ey","ibia","ibou","ican","ida","igator","illa","ing","ingale","ingo","ish","itar","eleon","ypus","ite","ium","oceros","oda","odile","odo","onite","oon","oose","opotamus","opus","ora","orb","os","osaur","ossum","oth","owary","oyote","uar","uin","uito","upine","utor","ybara","yte"};
        static string[] nm8 = new string[] {"bat","bil","boon","bug","dine","fly","meleon","guin","hawk","hog","hopper","key","king","ling","madillo","mingo","mite","nea","pecker","phant","phin","pie","pion","quito","raffe","ray","rilla","roach","ron","sel","ster","tile","topus","vark","whale","wing","zelle"};
        static string[] nm9 = new string[] {"b","c","d","f","g","k","l","m","n","p","q","r","s","t","v","w","x","z","b","c","d","f","g","k","l","m","n","p","q","r","s","t","v","w","x","z","br","cr","dr","gr","kr","pr","sr","st","str","bl","cl","fl","gl","kl","pl","sl","vl"};

        public static string GetName1(int seed)
        {
            var rand = new System.Random(seed);

            int rnd1 = rand.Next(nm1.Length);
            int rnd2 = rand.Next(nm2.Length);
            int rnd3 = rand.Next(nm3.Length);
            int rnd4 = rand.Next(nm4.Length);
            int rnd5 = rand.Next(nm5.Length);
            if(rnd4 < 2){
                rnd5 = 0;
            }
            return nm1[rnd1] + nm2[rnd2] + nm3[rnd3] + nm4[rnd4] + nm5[rnd5];
        }
        public static string GetName2(int seed)
        {
            var rand = new System.Random(seed);

            int rnd1 = rand.Next(nm6.Length);
            int rnd2 = rand.Next(nm3.Length);
            int rnd3 = rand.Next(nm2.Length);
            int rnd4 = rand.Next(nm3.Length);
            int rnd5 = rand.Next(nm4.Length);
            int rnd6 = rand.Next(nm5.Length);
            if(rnd5 < 2){
                rnd6 = 0;
            }
            return nm6[rnd1] + nm3[rnd2] + nm2[rnd3] + nm3[rnd4] + nm4[rnd5] + nm5[rnd6];
        }
        public static string GetName3(int seed)
        {
            var rand = new System.Random(seed);

            int rnd1 = rand.Next(nm9.Length);
            int rnd2 = rand.Next(nm3.Length);
            int rnd3 = rand.Next(nm4.Length);
            while(rnd3 < 2){
                rnd3 = rand.Next(nm4.Length);
            }
            int rnd4 = rand.Next(nm7.Length);
            return nm9[rnd1] + nm3[rnd2] + nm4[rnd3] + nm7[rnd4];
        }
        public static string GetName4(int seed)
        {
            var rand = new System.Random(seed);
            int rnd1 = rand.Next(nm9.Length);
            int rnd2 = rand.Next(nm3.Length);
            int rnd3 = rand.Next(nm4.Length);
            int rnd4 = rand.Next(nm5.Length);
            if(rnd3 < 2){
                rnd4 = 0;
            }
            int rnd5 = rand.Next(nm8.Length);
            return nm9[rnd1] + nm3[rnd2] + nm4[rnd3] + nm5[rnd4] + nm8[rnd5];
        }
        public static string GetName5(int seed)
        {
            var rand = new System.Random(seed);

            int rnd1 = rand.Next(nm9.Length);
            int rnd2 = rand.Next(nm3.Length);
            int rnd3 = rand.Next(nm2.Length);
            int rnd4 = rand.Next(nm3.Length);
            int rnd5 = rand.Next(nm4.Length);
            int rnd6 = rand.Next(nm5.Length);
            if(rnd5 < 2){
                rnd6 = 0;
            }
            return nm9[rnd1] + nm3[rnd2] + nm2[rnd3] + nm3[rnd4] + nm4[rnd5] + nm5[rnd6];
        }
        public static string GetName6(int seed)
        {
            var rand = new System.Random(seed);

            int rnd1 = rand.Next(nm6.Length);
            int rnd2 = rand.Next(nm7.Length);
            return nm6[rnd1] + nm7[rnd2];
        }

// var nm1 = ["Alba","Alli","Ana","Ante","Arma","Barra","Bea","Buffa","Cani","Cari","Cate","Coyo","Cri","Cro","Croco","Drago","Ele","Feli","Fla","Flami","Gaze","Gira","Hexa","Hone","Jagu","Komo","Leo","Locu","Mana","Moo","Pa","Pea","Peli","Phea","Porcu","Rhi","Rhino","Sala","Sco","Sku","Sna","Snai","Spa","Spi","Squi","Sti","Toa","Ursa","Vi","Wea","Wha","Woo"];
// var nm2 = ["b","c","d","f","g","k","l","m","n","p","q","r","s","t","v","w","x","z","b","c","d","f","g","k","l","m","n","p","q","r","s","t","v","w","x","z","br","cr","dr","gr","kr","pr","sr","st","str","bl","cl","fl","gl","kl","pl","sl","vl","cs","ds","fs","gs","ks","ls","ms","ns","ps","rs","ss","ts","bb","cc","dd","ff","gg","kk","ll","mm","nn","pp","rr","ss","tt","ww","zz"];
// var nm3 = ["a","o","i","e","u","aa","oo","ee","au","ou","ea","eo"];
// var nm4 = ["","","c","d","k","l","m","n","p","r","s","t","x","cs","ks","ps","rs","ts","st"];
// var nm5 = ["","","","","","a","o","i","e","u","ee","ea","eo"];

// var nm6 = ["Alb","All","Alp","Ant","Arach","Arm","Bab","Badg","Barr","Beav","Bis","Buff","Cam","Cat","Chick","Cobr","Coy","Croc","Dol","Don","Drag","Eag","El","Eleph","Fal","Falc","Fer","Flam","Gaz","Ger","Gir","Guin","Hedg","Hex","Hipp","Hor","Horn","Humm","Hyen","Jag","Kang","Koal","Kom","Komod","Leop","Lob","Mag","Mall","Mant","Mon","Mong","Mos","Mosq","Mul","Oct","Ost","Pan","Pand","Parr","Pel","Pen","Peng","Pon","Por","Quad","Rab","Rabb","Rac","Racc","Rhin","Sal","Sar","Scor","Ser","Serp","Skun","Snak","Spar","Sparr","Spid","Stin","Sting","Ter","Term","Tetr","Tuc","Tur","Turt","Vul","Vult","Wal","Wall","War","Wart","Wol","Wolv","Wom","Wor","Zeb"];
// var nm7 = ["abura","aby","acle","acuda","adger","adillo","alo","amander","amel","ander","anzee","api","arak","aroo","aros","atee","atross","ecta","een","ela","elope","ena","eon","ephant","erine","erpillar","eton","ey","ibia","ibou","ican","ida","igator","illa","ing","ingale","ingo","ish","itar","eleon","ypus","ite","ium","oceros","oda","odile","odo","onite","oon","oose","opotamus","opus","ora","orb","os","osaur","ossum","oth","owary","oyote","uar","uin","uito","upine","utor","ybara","yte"];
// var nm8 = ["bat","bil","boon","bug","dine","fly","meleon","guin","hawk","hog","hopper","key","king","ling","madillo","mingo","mite","nea","pecker","phant","phin","pie","pion","quito","raffe","ray","rilla","roach","ron","sel","ster","tile","topus","vark","whale","wing","zelle"];

// var nm9 = ["b","c","d","f","g","k","l","m","n","p","q","r","s","t","v","w","x","z","b","c","d","f","g","k","l","m","n","p","q","r","s","t","v","w","x","z","br","cr","dr","gr","kr","pr","sr","st","str","bl","cl","fl","gl","kl","pl","sl","vl"];

// function nameGen(){
//     $('#placeholder').css('textTransform', 'capitalize');
//     var br = "";
//     var element = document.createElement("div");
//     element.setAttribute("id", "result");
    
//     for(i = 0; i < 12; i++){
//         if(i < 2){
//             rnd = Math.floor(Math.random() * nm1.length);
//             rnd2 = Math.floor(Math.random() * nm2.length);
//             rnd3 = Math.floor(Math.random() * nm3.length);
//             rnd4 = Math.floor(Math.random() * nm4.length);
//             rnd5 = Math.floor(Math.random() * nm5.length);
//             if(rnd4 < 2){
//                 rnd5 = 0;
//             }
//             nm = nm1[rnd] + nm2[rnd2] + nm3[rnd3] + nm4[rnd4] + nm5[rnd5];
//         }else if(i < 4){
//             rnd = Math.floor(Math.random() * nm6.length);
//             rnd2 = Math.floor(Math.random() * nm3.length);
//             rnd3 = Math.floor(Math.random() * nm2.length);
//             rnd4 = Math.floor(Math.random() * nm3.length);
//             rnd5 = Math.floor(Math.random() * nm4.length);
//             rnd6 = Math.floor(Math.random() * nm5.length);
//             if(rnd5 < 2){
//                 rnd6 = 0;
//             }
//             nm = nm6[rnd] + nm3[rnd2] + nm2[rnd3] + nm3[rnd4] + nm4[rnd5] + nm5[rnd6];
//         }else if(i < 6){
//             rnd = Math.floor(Math.random() * nm9.length);
//             rnd2 = Math.floor(Math.random() * nm3.length);
//             rnd3 = Math.floor(Math.random() * nm4.length);
//             while(rnd3 < 2){
//                 rnd3 = Math.floor(Math.random() * nm4.length);
//             }
//             rnd4 = Math.floor(Math.random() * nm7.length);
//             nm = nm9[rnd] + nm3[rnd2] + nm4[rnd3] + nm7[rnd4];
//         }else if(i < 8){
//             rnd = Math.floor(Math.random() * nm9.length);
//             rnd2 = Math.floor(Math.random() * nm3.length);
//             rnd3 = Math.floor(Math.random() * nm4.length);
//             rnd4 = Math.floor(Math.random() * nm5.length);
//             if(rnd3 < 2){
//                 rnd4 = 0;
//             }
//             rnd5 = Math.floor(Math.random() * nm8.length);
//             nm = nm9[rnd] + nm3[rnd2] + nm4[rnd3] + nm5[rnd4] + nm8[rnd5];
//         }else if(i < 10){
//             rnd = Math.floor(Math.random() * nm9.length);
//             rnd2 = Math.floor(Math.random() * nm3.length);
//             rnd3 = Math.floor(Math.random() * nm2.length);
//             rnd4 = Math.floor(Math.random() * nm3.length);
//             rnd5 = Math.floor(Math.random() * nm4.length);
//             rnd6 = Math.floor(Math.random() * nm5.length);
//             if(rnd5 < 2){
//                 rnd6 = 0;
//             }
//             nm = nm9[rnd] + nm3[rnd2] + nm2[rnd3] + nm3[rnd4] + nm4[rnd5] + nm5[rnd6];
//         }else{
//             rnd = Math.floor(Math.random() * nm6.length);
//             rnd2 = Math.floor(Math.random() * nm7.length);
//             nm = nm6[rnd] + nm7[rnd2];
//         }
//         br = document.createElement('br');	
//         element.appendChild(document.createTextNode(nm));
//         element.appendChild(br);
//     }
//     if(document.getElementById("result")){
//         document.getElementById("placeholder").removeChild(document.getElementById("result"));
//     }		
//     document.getElementById("placeholder").appendChild(element);
// }
    }
}
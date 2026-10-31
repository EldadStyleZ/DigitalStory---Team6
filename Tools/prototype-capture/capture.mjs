// Prints PrototypeValues.cs, the answers the C# tests check against.
// Holds the HTML prototype's own rules, copied with drawing and audio removed.
// Run from the repo root:
//   node Tools/prototype-capture/capture.mjs > Assets/_Project/Tests/EditMode/Prototype/PrototypeValues.cs
const W=2000,H=1300,TAU=Math.PI*2,RX=W-250,SHALLOW=70;
const edge=y=>RX+18*Math.sin(y/90)+9*Math.sin(y/31+2);
const clamp=(v,a,b)=>v<a?a:v>b?b:v;
function mulberry(a){return function(){a|=0;a=a+0x6D2B79F5|0;let t=Math.imul(a^a>>>15,1|a);t=t+Math.imul(t^t>>>7,61|t)^t;return((t^t>>>14)>>>0)/4294967296}}
const LEVELS=[{rats:70,quota:45,cats:0,time:130},{rats:170,quota:115,cats:1,time:150},{rats:290,quota:210,cats:3,time:180},{rats:430,quota:330,cats:5,time:210}];
let houses=[],lamps=[],rats=[],cats=[],inters=[],cw=230,ch=190,cols=0,rows=0,L=null;
const P={x:0,y:0,face:Math.PI,moving:false};
let random=Math.random;
let timeLeft=0,drowned=0,lost=0,alive=0,breath=100,exhausted=false,playing=false,curHole=-1,mode='',musicR=210;

function collide(e,rad){
 for(const h of houses){
  if(e.x+rad<h.x||e.x-rad>h.x+h.w||e.y+rad<h.y||e.y-rad>h.y+h.h)continue;
  const px=clamp(e.x,h.x,h.x+h.w),py=clamp(e.y,h.y,h.y+h.h);
  const dx=e.x-px,dy=e.y-py,d2=dx*dx+dy*dy;
  if(d2>1e-6){const d=Math.sqrt(d2);if(d<rad){const nx=dx/d,ny=dy/d;e.x+=nx*(rad-d);e.y+=ny*(rad-d);
    if(e.vx!==undefined){const vn=e.vx*nx+e.vy*ny;if(vn<0){e.vx-=vn*nx;e.vy-=vn*ny}}}}
  else{const l=e.x-h.x,r=h.x+h.w-e.x,t=e.y-h.y,b=h.y+h.h-e.y,m=Math.min(l,r,t,b);
    if(m===l)e.x=h.x-rad;else if(m===r)e.x=h.x+h.w+rad;else if(m===t)e.y=h.y-rad;else e.y=h.y+h.h+rad}
 }
}

function genLevel(i,withCats=true){
 L=LEVELS[i];const rng=mulberry(1234+i*977);
 houses=[];lamps=[];inters=[];rats=[];cats=[];lost=0;
 const st=64;
 cols=Math.floor((RX-170)/cw);rows=Math.ceil(H/ch);
 const H_=(x,y,w,h)=>({x,y,w,h,c:Math.floor(rng()*6),chim:rng()<.6,cx:rng(),cy:rng()});
 for(let gx=0;gx<cols;gx++)for(let gy=0;gy<rows;gy++){
  if(rng()<.15)continue;
  const x0=gx*cw+st/2,y0=gy*ch+st/2,bw=cw-st,bh=Math.min(ch-st,H-y0-st/2);
  if(bh<40)continue;
  const k=rng();
  if(k<.4){const sw=Math.round(bw*(.35+rng()*.3));houses.push(H_(x0,y0,sw,bh),H_(x0+sw,y0,bw-sw,bh))}
  else if(k<.65){const sh=Math.round(bh*(.4+rng()*.2));houses.push(H_(x0,y0,bw,sh),H_(x0,y0+sh,bw,bh-sh))}
  else houses.push(H_(x0,y0,bw,bh));
 }
 for(let gx=0;gx<=cols;gx++)for(let gy=0;gy<=rows;gy++){
  const p={x:gx*cw,y:Math.min(gy*ch,H-24),gx,gy};inters.push(p);
  if(rng()<.3)lamps.push({x:p.x+14,y:p.y+14});
 }
 for(let y=80;y<H;y+=210)lamps.push({x:RX-110,y:y+rng()*50});
 P.x=RX-120;P.y=H/2;P.face=Math.PI;
 const nN=3+i*2,nests=[];
 while(nests.length<nN){const p=inters[Math.floor(rng()*inters.length)];if(p.gx<cols&&Math.hypot(p.x-P.x,p.y-P.y)>380)nests.push(p)}
 for(let k=0;k<L.rats;k++){
  const ne=nests[k%nN],a=rng()*TAU,d=rng()*55;
  const r={x:ne.x+Math.cos(a)*d,y:ne.y+Math.sin(a)*d,vx:0,vy:0,wa:rng()*TAU,charm:0,alive:true,hx:ne.x,hy:ne.y,ph:rng()*TAU};
  collide(r,5);r.x=clamp(r.x,6,W);r.y=clamp(r.y,6,H-6);rats.push(r);
 }
 if(withCats)for(let k=0;k<L.cats;k++){
  let p;do{p=inters[Math.floor(rng()*inters.length)]}while(p.gx>=cols||Math.hypot(p.x-P.x,p.y-P.y)<450);
  cats.push({x:p.x,y:p.y,vx:0,vy:0,gx:p.gx,gy:p.gy,tx:p.x,ty:p.y,flee:0,eat:0,ph:rng()*TAU});
 }
 alive=L.rats;drowned=0;timeLeft=L.time;breath=100;exhausted=false;playing=false;curHole=-1;mode='';
 return {rng,nests};
}

const GC=36,GW=Math.ceil(W/GC),GH=Math.ceil(H/GC),gHead=new Int32Array(GW*GH);let gNext=new Int32Array(512);
const cell=(x,y)=>clamp(Math.floor(x/GC),0,GW-1)+clamp(Math.floor(y/GC),0,GH-1)*GW;
function drown(r){r.alive=false;alive--;drowned++}

function update(dt,input){
 timeLeft-=dt;
 let mvx=input.mx,mvy=input.my;
 const ml=Math.hypot(mvx,mvy),wet=P.x>edge(P.y),spd=wet?85:150;P.moving=ml>0;
 if(ml>0){mvx/=ml;mvy/=ml;P.x+=mvx*spd*dt;P.y+=mvy*spd*dt;
  const ta=Math.atan2(mvy,mvx),da=((ta-P.face+Math.PI)%TAU+TAU)%TAU-Math.PI;P.face+=da*Math.min(1,dt*12)}
 collide(P,11);P.y=clamp(P.y,12,H-12);P.x=clamp(P.x,12,edge(P.y)+SHALLOW-12);

 const ch_=input.hole,want=ch_>=0;
 if(exhausted&&breath>35)exhausted=false;
 playing=want&&!exhausted&&breath>0;
 if(playing){breath-=dt*11;if(breath<=0){breath=0;exhausted=true;playing=false}}
 else breath=Math.min(100,breath+dt*(want?9:22));
 curHole=playing?ch_:-1;mode=curHole<0?'':curHole<2?'call':curHole<4?'march':'scare';musicR=mode==='call'?240:mode==='march'?190:200;
 if(mode==='scare')for(const c of cats)if(!(c.sleep>0)&&Math.hypot(c.x-P.x,c.y-P.y)<musicR+60)c.flee=Math.max(c.flee,1.5);

 gHead.fill(-1);if(gNext.length<rats.length)gNext=new Int32Array(rats.length);
 for(let i=0;i<rats.length;i++){const r=rats[i];if(!r.alive)continue;const c=cell(r.x,r.y);gNext[i]=gHead[c];gHead[c]=i}

 for(let i=0;i<rats.length;i++){
  const r=rats[i];if(!r.alive)continue;
  let ax=0,ay=0,sx=0,sy=0,alx=0,aly=0,cox=0,coy=0,n=0,mc=0;
  const gx=clamp(Math.floor(r.x/GC),0,GW-1),gy=clamp(Math.floor(r.y/GC),0,GH-1);
  for(let oy=-1;oy<=1;oy++){const yy=gy+oy;if(yy<0||yy>=GH)continue;
   for(let ox=-1;ox<=1;ox++){const xx=gx+ox;if(xx<0||xx>=GW)continue;
    for(let j=gHead[xx+yy*GW];j!==-1;j=gNext[j]){if(j===i)continue;const o=rats[j];
     const dx=r.x-o.x,dy=r.y-o.y,d2=dx*dx+dy*dy;if(d2>1296)continue;
     const d=Math.sqrt(d2)||.01;if(d<13){sx+=dx/d*(13-d);sy+=dy/d*(13-d)}
     alx+=o.vx;aly+=o.vy;cox+=o.x;coy+=o.y;n++;if(o.charm>mc)mc=o.charm}}}
  ax+=sx*30;ay+=sy*30;
  if(n){const k=r.charm>.3?.4:1;ax+=(alx/n-r.vx)*.8*k;ay+=(aly/n-r.vy)*.8*k;ax+=(cox/n-r.x)*.9*k;ay+=(coy/n-r.y)*.9*k}
  r.wa+=(random()-.5)*5*dt;ax+=Math.cos(r.wa)*45;ay+=Math.sin(r.wa)*45;

  const pdx=P.x-r.x,pdy=P.y-r.y,pd=Math.hypot(pdx,pdy)||1;
  if(mode==='scare'&&pd<musicR){r.charm=Math.max(0,r.charm-dt*3);const f=700*(1-pd/musicR);ax-=pdx/pd*f;ay-=pdy/pd*f}
  else if(playing&&pd<musicR)r.charm=Math.min(1,r.charm+dt*(2.2*(1-pd/musicR)+.6));
  else if(playing&&mode!=='scare'&&mc>.6)r.charm=Math.min(1,r.charm+dt*.55);
  else r.charm=Math.max(0,r.charm-dt*.22);

  if(r.charm>.02){
   let tx=P.x,ty=P.y,stop=48;
   if(mode==='march'){tx=P.x+Math.cos(P.face)*150;ty=P.y+Math.sin(P.face)*150;stop=10}
   const dx=tx-r.x,dy=ty-r.y,d=Math.hypot(dx,dy)||1,sp=(80+110*r.charm)*(d>stop?1:d/stop);
   ax+=(dx/d*sp-r.vx)*3*r.charm;ay+=(dy/d*sp-r.vy)*3*r.charm}
  else{const hx=r.hx-r.x,hy=r.hy-r.y,hd=Math.hypot(hx,hy);if(hd>90){ax+=hx/hd*35;ay+=hy/hd*35}}
  const ed=edge(r.y);if(r.charm<.4&&r.x>ed-90)ax-=(r.x-(ed-90))*7;
  for(const c of cats){if(c.sleep>0)continue;const dx=r.x-c.x,dy=r.y-c.y,d=Math.hypot(dx,dy)||1;if(d<130){ax+=dx/d*520*(1-d/130);ay+=dy/d*520*(1-d/130);r.charm=Math.max(0,r.charm-dt*1.3)}}

  r.vx+=ax*dt;r.vy+=ay*dt;
  const max=80+110*r.charm,s=Math.hypot(r.vx,r.vy);if(s>max){r.vx*=max/s;r.vy*=max/s}
  r.x+=r.vx*dt;r.y+=r.vy*dt;collide(r,5);
  if(r.x<5){r.x=5;r.vx=Math.abs(r.vx)}if(r.y<5){r.y=5;r.vy=Math.abs(r.vy)}if(r.y>H-5){r.y=H-5;r.vy=-Math.abs(r.vy)}
  if(r.x>ed+4)drown(r);
 }

 for(const c of cats){
  if(c.sleep>0){c.sleep-=dt;c.vx*=.9;c.vy*=.9;continue}
  let dvx=0,dvy=0,sp=0;c.eat=Math.max(0,c.eat-dt);c.flee=Math.max(0,c.flee-dt);
  if(c.flee>0){const dx=c.x-P.x,dy=c.y-P.y,d=Math.hypot(dx,dy)||1;dvx=dx/d;dvy=dy/d;sp=175;c.hunt=false}
  else{
   let best=null,bd=220;
   if(c.eat<=0)for(const r of rats){if(!r.alive)continue;const d=Math.abs(r.x-c.x)+Math.abs(r.y-c.y);if(d<bd){bd=d;best=r}}
   if(best){const dx=best.x-c.x,dy=best.y-c.y,d=Math.hypot(dx,dy)||1;dvx=dx/d;dvy=dy/d;sp=140;c.hunt=true;
    if(d<11){best.alive=false;alive--;lost++;c.eat=1.6}}
   else{
    if(c.hunt){c.hunt=false;c.gx=clamp(Math.round(c.x/cw),0,cols-1);c.gy=clamp(Math.round(c.y/ch),0,rows);c.tx=c.gx*cw;c.ty=Math.min(c.gy*ch,H-24)}
    const dx=c.tx-c.x,dy=c.ty-c.y,d=Math.hypot(dx,dy);
    if(d<16){const opts=[[1,0],[-1,0],[0,1],[0,-1]].map(([a,b])=>[c.gx+a,c.gy+b]).filter(([a,b])=>a>=0&&a<cols&&b>=0&&b<=rows);
     const o=opts[Math.floor(random()*opts.length)];c.gx=o[0];c.gy=o[1];c.tx=c.gx*cw;c.ty=Math.min(c.gy*ch,H-24)}
    else{dvx=dx/d;dvy=dy/d;sp=70}
   }
  }
  c.vx+=(dvx*sp-c.vx)*Math.min(1,dt*4);c.vy+=(dvy*sp-c.vy)*Math.min(1,dt*4);
  c.x+=c.vx*dt;c.y+=c.vy*dt;collide(c,9);
  const ed=edge(c.y);if(c.x>ed-40){c.x=ed-40;c.vx=-Math.abs(c.vx)}
  c.x=clamp(c.x,8,W);c.y=clamp(c.y,8,H-8);
 }
}

const num=v=>Number.isInteger(v)?v+'d':String(v)+'d';
const out=[];
const line=s=>out.push(s);
line('// Generated by Tools/prototype-capture/capture.mjs. Do not edit by hand.');
line('namespace DigitalStory.Tests.EditMode');line('{');
line('    /// <summary>Values captured by running the prototype\'s own rules.</summary>');
line('    internal static class PrototypeValues');line('    {');

{const r=mulberry(1234);line(`        public static readonly double[] RandomFromSeed1234 = { ${[0,1,2,3,4].map(()=>num(r())).join(', ')} };`);}
{const r=mulberry(0);line(`        public static readonly double[] RandomFromSeed0 = { ${[0,1,2].map(()=>num(r())).join(', ')} };`);}
line(`        public static readonly double[] RiverSampleY = { 0d, 90d, 333d, 650d, 1299.5d };`);
line(`        public static readonly double[] RiverEdgeAtSample = { ${[0,90,333,650,1299.5].map(y=>num(edge(y))).join(', ')} };`);
line('');
line('        /// <summary>One generated town, with sums that change if any value or random call differs.</summary>');
line('        public readonly struct Night');line('        {');
const fields=['int Columns','int Rows','int HouseCount','int JunctionCount','int LampCount','double HouseSum','double RoofSum','int ChimneyCount','double ChimneyOffsetSum','double LampSum','double NestSum','double RatPositionSum','double RatWanderSum','double RatPhaseSum','double NextRandom'];
for(const f of fields)line(`            public readonly ${f};`);
line('');
line(`            public Night(${fields.map(f=>{const [t,n]=f.split(' ');return t+' '+n[0].toLowerCase()+n.slice(1)}).join(', ')})`);
line('            {');
for(const f of fields){const n=f.split(' ')[1];line(`                ${n} = ${n[0].toLowerCase()+n.slice(1)};`)}
line('            }');line('        }');line('');
line('        public static readonly Night[] Nights =');line('        {');
for(let i=0;i<4;i++){
 const {rng,nests}=genLevel(i,false);
 const sum=(a,f)=>a.reduce((s,v)=>s+f(v),0);
 const v=[cols,rows,houses.length,inters.length,lamps.length,
  num(sum(houses,h=>h.x+h.y*3+h.w*5+h.h*7)),num(sum(houses,h=>h.c)),houses.filter(h=>h.chim).length,num(sum(houses,h=>h.cx+h.cy)),
  num(sum(lamps,l=>l.x+l.y*3)),num(sum(nests,p=>p.x+p.y*3)),num(sum(rats,r=>r.x+r.y*3)),num(sum(rats,r=>r.wa)),num(sum(rats,r=>r.ph)),num(rng())];
 line(`            new Night(${v.join(', ')}),`);
}
line('        };');line('');

line('        /// <summary>The state after one phase of the scripted run.</summary>');
line('        public readonly struct Snapshot');line('        {');
const sf=['int Ticks','float MoveX','float MoveY','int Hole','double PiperX','double PiperY','double PiperFacing','double Breath','double TimeLeft','double MeanRatX','double MeanRatY','double MeanCharm','int Drowned'];
for(const f of sf)line(`            public readonly ${f};`);
line('');
line(`            public Snapshot(${sf.map(f=>{const [t,n]=f.split(' ');return t+' '+n[0].toLowerCase()+n.slice(1)}).join(', ')})`);
line('            {');
for(const f of sf){const n=f.split(' ')[1];line(`                ${n} = ${n[0].toLowerCase()+n.slice(1)};`)}
line('            }');line('        }');line('');
line('        public const int ScriptedRandomSeed = 99;');line('        public const float ScriptedStep = 1f / 60f;');line('');
line('        public static readonly Snapshot[] ScriptedRun =');line('        {');
genLevel(0);random=mulberry(99);
const script=[[44,0,1,-1],[280,-1,0,-1],[200,-1,0,0],[100,0,0,0],[90,1,0,2],[60,0,0,4],[150,0,0,-1],[120,-1,0,0],[200,1,0,1],[180,1,0,-1],[260,1,0,0],[120,1,0,-1],[240,1,0,2]];
for(const [t,mx,my,hole] of script){
 for(let k=0;k<t;k++)update(1/60,{mx,my,hole});
 const live=rats.filter(r=>r.alive),m=f=>live.reduce((s,r)=>s+f(r),0)/live.length;
 line(`            new Snapshot(${t}, ${mx}f, ${my}f, ${hole}, ${num(P.x)}, ${num(P.y)}, ${num(P.face)}, ${num(breath)}, ${num(timeLeft)}, ${num(m(r=>r.x))}, ${num(m(r=>r.y))}, ${num(m(r=>r.charm))}, ${drowned}),`);
}
line('        };');line('');
const RUNS=30,phases=script.length,xs=phases&&script.map(()=>[]),cs=script.map(()=>[]),ds=script.map(()=>[]);
for(let run=0;run<RUNS;run++){
 genLevel(0);random=mulberry(1000+run);
 script.forEach(([t,mx,my,hole],i)=>{
  for(let k=0;k<t;k++)update(1/60,{mx,my,hole});
  const live=rats.filter(r=>r.alive),m=f=>live.reduce((a,r)=>a+f(r),0)/live.length;
  xs[i].push(m(r=>r.x));cs[i].push(m(r=>r.charm));ds[i].push(drowned);
 });
}
const mean=a=>a.reduce((p,v)=>p+v,0)/a.length;
const err=a=>{const m=mean(a);return Math.sqrt(a.reduce((p,v)=>p+(v-m)*(v-m),0)/(a.length-1)/a.length)};
const arr=(name,rows,f)=>line(`        public static readonly double[] ${name} = { ${rows.map(r=>num(f(r))).join(', ')} };`);
line('        // Averages over many runs, because one flock run is too sensitive to rounding to compare exactly.');
line(`        public const int AverageRunCount = ${RUNS};`);line('        public const int AverageFirstSeed = 1000;');
arr('AverageMeanRatX',xs,mean);arr('AverageMeanRatXError',xs,err);
arr('AverageMeanCharm',cs,mean);arr('AverageMeanCharmError',cs,err);
arr('AverageDrowned',ds,mean);arr('AverageDrownedError',ds,err);
line('');

line('        // Where each night\'s cats start, as a sum that changes if any value or random call differs.');
{const sums=[],after=[];
 for(let i=0;i<4;i++){const {rng}=genLevel(i);sums.push(cats.reduce((s,c)=>s+c.x+c.y*3+c.ph,0));after.push(rng())}
 line(`        public static readonly double[] CatStartSum = { ${sums.map(num).join(', ')} };`);
 line(`        public static readonly double[] RandomAfterCats = { ${after.map(num).join(', ')} };`);}
line('');

line('        // One cat alone in the second night\'s town, as x and y after each phase.');
{const ticks=120,phases=6,seen=[];
 genLevel(1);rats=[];random=mulberry(99);
 for(let s=0;s<phases;s++){for(let k=0;k<ticks;k++)update(1/60,{mx:0,my:0,hole:-1});seen.push(cats[0].x,cats[0].y)}
 line(`        public const int CatPatrolTicks = ${ticks};`);
 line(`        public static readonly double[] CatPatrol = { ${seen.map(num).join(', ')} };`);}
line('');

line('        // The same scripted walk on the second night, where a cat hunts, averaged over many runs.');
{const lostRuns=script.map(()=>[]),charmRuns=script.map(()=>[]);
 for(let run=0;run<RUNS;run++){
  genLevel(1);random=mulberry(1000+run);
  script.forEach(([t,mx,my,hole],i)=>{
   for(let k=0;k<t;k++)update(1/60,{mx,my,hole});
   const live=rats.filter(r=>r.alive);
   lostRuns[i].push(lost);charmRuns[i].push(live.reduce((a,r)=>a+r.charm,0)/live.length);
  });
 }
 arr('CatNightAverageLost',lostRuns,mean);arr('CatNightAverageLostError',lostRuns,err);
 arr('CatNightAverageMeanCharm',charmRuns,mean);arr('CatNightAverageMeanCharmError',charmRuns,err);}
line('    }');line('}');
console.log(out.join('\n'));

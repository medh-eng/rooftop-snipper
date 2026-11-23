// Checks every crowd route (same Hermite maths as HarborCrowdTraffic.Path) against the
// concourse's solid objects from HarborCrowdReview.BuildConcourse. Run: node tools/route-check.cjs
const W = [
 [-12.4,-1.2,-8.1,-1.2,-5.1,-2.0,-.5,-2.9,4.0,-1.6,8.1,-1.2,12.5,-1.2],
 [12.5,1.7,8.2,1.7,5.1,3.1,1.0,2.6,-3.0,3.8,-7.9,4.1,-12.4,3.8],
 [0,8.2,0,5.4,-.8,3.6,-2.7,2.5,-5.1,1.6,-8.4,-.5,-12.4,-.7],
 [-12.4,-3.2,-8.5,-1.7,-5.5,-2.8,-2.8,-1.4,-1.5,1.2,-.4,3.9,0,8.2],
 [12.5,-1.2,8.2,-.7,6.2,-.4,4.3,-.8,3.9,-3.5,4.4,-5.7,4.4,-9.0],
 [4.4,-9,4.6,-5.9,6.6,-4.0,8.7,-2.8,12.5,-2.8],
 [4.4,-9,3.6,-5.2,1.9,-3.6,.4,-1.1,-.2,1.8,-.3,4.2,0,8.2],
 [.2,8.2,.5,4.2,2.2,3.3,4.4,3.7,8.3,1.8,12.5,1.8],
 [-12.4,.7,-8.5,.6,-6.7,-1.2,-3.2,-3.5,.6,-4.4,3.3,-5.6,4.4,-9],
 [4.4,-9,2.6,-5.3,-.8,-3.8,-4.9,-3.9,-8.4,-1.6,-12.4,-1.7]];
// boxes: [name, cx, cz, sx, sz]; circles: [name, cx, cz, r]
const B = [
 ['terminal left wing',-4,5.3,4.2,1.4],['terminal right wing',4,5.3,4.2,1.4],
 ['terminal west glass',-1.36,5.93,.75,.08],['terminal east glass',1.36,5.93,.75,.08],
 ['kiosk',-6.2,2.4,1,1.1],['bench',5.5,-2.7,1.7,.55],['bench back',5.5,-2.95,1.7,.12],
 ['pier west rail',-2.55,-6.65,11.8,.28],['pier east rail',7.12,-6.65,3.5,.28],
 ['obs facade',-8,-5,1,4]];
const C = [['west pillar',-4.15,.7,.13],['east pillar',2.82,1.1,.13]];
const R = .30; // body radius incl. shoulders/props
function path(c){const n=[];for(let i=0;i<c.length/2-1;i++){const P=k=>[c[2*Math.max(0,Math.min(c.length/2-1,k))],c[2*Math.max(0,Math.min(c.length/2-1,k))+1]];
 const a=P(i-1),b=P(i),cc=P(i+1),d=P(i+2);const seg=Math.hypot(cc[0]-b[0],cc[1]-b[1]);
 const cl=v=>{const m=Math.hypot(v[0],v[1]);return m>seg?[v[0]*seg/m,v[1]*seg/m]:v};
 const l=cl([(cc[0]-a[0])*.5,(cc[1]-a[1])*.5]),r=cl([(d[0]-b[0])*.5,(d[1]-b[1])*.5]);
 for(let j=0;j<12;j++){const t=j/12,t2=t*t,t3=t2*t;const f=(u,v,w,x,y)=>u*b[y]+v*l[y]+w*cc[y]+x*r[y];
  n.push([0,1].map(y=>(2*t3-3*t2+1)*b[y]+(t3-2*t2+t)*l[y]+(-2*t3+3*t2)*cc[y]+(t3-t2)*r[y]));}}
 n.push([c[c.length-2],c[c.length-1]]);return n;}
function distBox(p,[_,cx,cz,sx,sz]){const dx=Math.max(Math.abs(p[0]-cx)-sx/2,0),dz=Math.max(Math.abs(p[1]-cz)-sz/2,0);return Math.hypot(dx,dz);}
// Same gate rule as CaseCrowd.GatedControls: ferry routes pass the gangway centre line.
function gated(c){c=c.slice();const f=(x,z)=>Math.abs(x-4.4)<.01&&Math.abs(z+9)<.01;
 if(f(c[0],c[1]))c.splice(2,0,4.4,-7.0);const n=c.length;if(f(c[n-2],c[n-1]))c.splice(n-2,0,4.4,-7.0);return c;}
let bad=0;W.forEach((c,i)=>{const pts=path(gated(c));let worst=[1e9,''];
 for(const p of pts){for(const b of B){const d=distBox(p,b);if(d<worst[0])worst=[d,b[0]+' @'+p.map(v=>v.toFixed(1))];}
  for(const k of C){const d=Math.hypot(p[0]-k[1],p[1]-k[2])-k[3];if(d<worst[0])worst=[d,k[0]+' @'+p.map(v=>v.toFixed(1))];}}
 const ok=worst[0]>=R;if(!ok)bad++;console.log(`route ${i}: nearest solid ${worst[0].toFixed(2)} m (${worst[1]}) ${ok?'ok':'TOO CLOSE'}`);});
process.exit(bad?1:0);
